import { CommonModule, NgIf } from '@angular/common';
import { Component, HostListener, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Pays } from 'src/app/_models/pays';
import { SocieteCreate } from 'src/app/_models/societe-create.model';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';
import { LoaderService } from 'src/app/_services/loader.service';
import { OverlayModalService } from 'src/app/_services/overlay-modal.service';
import { PaysService } from 'src/app/_services/pays.service';
import { ProjetService } from 'src/app/_services/projet.service';
import { SocieteService } from 'src/app/_services/societe.service';
import { ContractDialogComponent } from 'src/app/contract-dialog/contract-dialog.component';
import { ClientCreate } from 'src/app/DTOs/client-create.model';
import { PaysModalComponent } from 'src/app/PaysFile/pays-modal/pays-modal.component';
import { concatMap, map, tap, catchError } from 'rxjs/operators';
import { of, throwError } from 'rxjs';
import { NgSelectModule } from '@ng-select/ng-select';
import { MatSelectModule } from '@angular/material/select';
import { ClientService } from 'src/app/_services/client.service';
import { RegisterClientDto } from 'src/app/DTOs/RegisterClientDto';
import { ProjetCreate } from 'src/app/DTOs/projet-create.model';

@Component({
  selector: 'app-ajouter-societe-wizard',
  imports: [ReactiveFormsModule, CommonModule, FormsModule, NgSelectModule, MatSelectModule],
  templateUrl: './ajouter-societe-wizard.component.html',
  styleUrl: './ajouter-societe-wizard.component.css'
})
export class AjouterSocieteWizardComponent implements OnInit {
  wizardForm!: FormGroup;
  step = 1;
  isLoading = false;
  isCompleted = false;
  paysList: any[] = [];
  chefsList: User[] = [];
  selectedCountry?: any;
  selectedCountrySociete?: Pays;
  createdSocieteId!: number;
  createdProjetId!: number;

  filteredPays: Pays[] = [];
  paysSearchTerm = '';
  isPaysDropdownOpen = false;

  societesList: any[] = [];
  projetsList: any[] = [];

  serverErrors = {
    societe: '' as string,
    projet: '' as string,
    client: '' as string,
  };
  

  constructor(
    private fb: FormBuilder,
    private societeService: SocieteService,
    private projetService: ProjetService,
    private accountService: AccountService,
    private clientService: ClientService,
    private paysService: PaysService,
    private loaderService: LoaderService,
    private overlayModalService: OverlayModalService,
    private toastr: ToastrService,
    private router: Router,
    private dialog: MatDialog
  ) {
    this.loaderService.isLoading$.subscribe(l => this.isLoading = l);
  }

  ngOnInit(): void {
    this.buildForm();
    this.loadPays();
    this.loadChefs();
    this.loadSocietes();

    this.filteredPays = this.paysList;

    // update prefix on country change
    this.clientGroup.get('pays')!.valueChanges.subscribe(id => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +id);
      this.clientGroup.get('numTelephone')!.reset();
    });

    this.societeGroup.get('paysId')!.valueChanges.subscribe(id => {
      // on récupère les infos du pays sélectionné
      this.selectedCountrySociete = this.paysList.find(p => p.idPays === +id);
      // on nettoie le champ téléphone pour forcer la saisie au bon format
      this.societeGroup.get('telephone')!.reset();
    });

    this.clientGroup.get('societeId')!.valueChanges.subscribe(id => {
      this.clientGroup.patchValue({ projetIds: [] });
      this.projetsList = [];
      if (id) this.loadProjetsBySociete(id);
    });
  }


  private buildForm(): void {
    this.wizardForm = this.fb.group({
      societe: this.fb.group({
        nom: ['', Validators.required],
        adresse: ['', Validators.required],
        telephone: ['', [
          Validators.required,
          Validators.pattern('^[0-9\\s]+$')
        ]],
        paysId: ['', Validators.required],
        ville: ['', Validators.required],
        codePostal: ['', Validators.required]
      }),
      projet: this.fb.group({
        nom: ['', Validators.required],
        description: [''],
        chefProjetId: [null, Validators.required],
      }),
      client: this.fb.group({
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        pays: ['', Validators.required],
        numTelephone: ['', [Validators.required, Validators.pattern('^[0-9\\s]+$')]],
        societeId: [''],
        projetIds: [[]],
        actif: [true]
      })
    });
  }

  private loadPays(): void {
    this.paysService.getPays().subscribe({
      next: data => {
        this.paysList = data;
        this.filteredPays = data;
      },
      error: () => this.toastr.error('Erreur chargement pays')
    });
  }


  private loadChefs(): void {
    this.accountService.getUsersByRole('Chef de Projet')
      .subscribe({
        next: users => this.chefsList = users,
        error: () => this.toastr.error('Erreur chargement chefs de projet')
      });
  }


  submitAll(): void {
    // 0) Réinitialisation de tous les messages d'erreur
    this.serverErrors = { societe: '', projet: '', client: '' };
  
    // 1) Validation Angular du sous-formulaire client
    const clientForm = this.clientGroup;
    if (clientForm.invalid) {
      clientForm.markAllAsTouched();
      return;
    }
  
    // 2) Préparation du DTO Société
    const s = this.societeGroup.value as any;
    const socDto: SocieteCreate = {
      nom: s.nom,
      adresse: s.adresse,
      telephone: s.telephone.trim(),
      paysId: +s.paysId,
      ville: s.ville,
      codePostal: s.codePostal
    };
  
    // 3) Pipeline RxJS : addSociete → addProjet → register(client) avec rollback
    this.societeService.addSociete(socDto).pipe(
      // 3a) Création du projet lié
      concatMap(socResp => {
        this.createdSocieteId = socResp.id;
        const p = this.projetGroup.value as any;
        const projDto: ProjetCreate = {
          nom: p.nom,
          description: p.description,
          chefProjetId: +p.chefProjetId,
          societeId: this.createdSocieteId
        };
        return this.projetService.addProjet(projDto);
      }),
      // 3b) Tentative d'enregistrement du client
      concatMap(projResp => {
        this.createdProjetId = projResp.id;
        const c = clientForm.value as any;
        const clientDto: RegisterClientDto = {
          email: c.email,
          firstName: c.firstName,
          lastName: c.lastName,
          numTelephone: c.numTelephone.trim(),
          pays: +c.pays,
          societeId: this.createdSocieteId,
          actif: c.actif,
          projetIds: [this.createdProjetId]
        };
  
        return this.clientService.register(clientDto).pipe(
          catchError(err => {
            // 3c) Rollback : supprimer d'abord le projet, puis la société
            return this.projetService.deleteProjet(this.createdProjetId).pipe(
              concatMap(() => this.societeService.deleteSociete(this.createdSocieteId)),
              concatMap(() => throwError(() => err))
            );
          })
        );
      })
    )
    .subscribe({
      next: () => {
        this.isCompleted = true;
        this.toastr.success('Création réussie');
        this.router.navigate(['/home/Societes']);
      },
      error: (err: any) => {
        // 4) Affichage du message d'erreur métier ou générique
        this.serverErrors.client = typeof err === 'string'
          ? err
          : err.error || err.message || 'Erreur inconnue';
        this.loaderService.hideLoader();
      }
    });
  }
  

  prevStep(): void {
    this.step = Math.max(this.step - 1, 1);
  }
  nextStep(): void {
    if (this.step === 1) {
      this.serverErrors.societe = '';
      const sg = this.societeGroup;
      if (sg.invalid) {
        sg.markAllAsTouched();
        return;
      }
      this.societeService.validateSociete(sg.value).subscribe({
        next: () => this.step++,
        error: (err: any) => {
          this.serverErrors.societe = typeof err === 'string'
            ? err
            : err.message || 'Erreur validation société';
          this.loaderService.hideLoader();
        }
      });
    } else if (this.step === 2) {
      this.serverErrors.projet = '';
      const pg = this.projetGroup;
      if (pg.invalid) {
        pg.markAllAsTouched();
        return;
      }
      const dto = {
        ...pg.value,
        societeId: this.createdSocieteId   // créé **après** addSociete() dans submitAll
      };
      this.projetService.validateProjet(dto).subscribe({
        next: () => this.step++,
        error: (err: any) => {
          this.serverErrors.projet = typeof err === 'string'
            ? err
            : err.message || 'Erreur validation projet';
          this.loaderService.hideLoader();
        }
      });
    }
  }
  
  

  get societeGroup(): FormGroup {
    return this.wizardForm.get('societe') as FormGroup;
  }
  get projetGroup(): FormGroup {
    return this.wizardForm.get('projet') as FormGroup;
  }
  get clientGroup(): FormGroup {
    return this.wizardForm.get('client') as FormGroup;
  }

  public cleanupOnExit(): void {
    if (this.isCompleted) return;
    // Rien à faire si on est toujours à l'étape 1
    if (this.step === 1 || !this.createdSocieteId) {
      return;
    }

    // Si on est à l'étape 3 ou plus, on a créé un projet
    if (this.step > 2 && this.createdProjetId) {
      this.projetService.deleteProjet(this.createdProjetId).subscribe({
        next: () => this.deleteSociete(),
        error: () => this.deleteSociete()
      });
    } else {
      // Étape 2 (projet non créé) : suppression directe de la société
      this.deleteSociete();
    }
  }
  private deleteSociete(): void {
    this.societeService.deleteSociete(this.createdSocieteId).subscribe({
      next: () => { /* OK, la société est supprimée */ },
      error: () => { /* On continue quand même */ }
    });
  }

  @HostListener('window:beforeunload', ['$event'])
  beforeUnloadHandler(event: BeforeUnloadEvent): void {
    if (this.step > 1) {
      event.preventDefault();
      event.returnValue =
        'Vous allez perdre les données saisies. Voulez-vous vraiment quitter ?';
    }
  }

  @HostListener('window:unload')
  onUnload(): void {
    const base = this.societeService.apiUrl;

    // 1) Suppression du projet si on en a créé un
    if (this.step > 2 && this.createdProjetId) {
      const urlProj = `${base}/projets/supprimerProjet/${this.createdProjetId}`;
      fetch(urlProj, { keepalive: true });
      // ou, si vous préférez le hack "Image":
      // const img1 = new Image(); img1.src = urlProj;
    }

    // 2) Puis suppression de la société
    if (this.step > 1 && this.createdSocieteId) {
      const urlSoc = `${base}/delet/${this.createdSocieteId}`;
      fetch(urlSoc, { keepalive: true });
      // ou :
      // const img2 = new Image(); img2.src = urlSoc;
    }
  }

  togglePaysDropdown(): void {
    this.isPaysDropdownOpen = !this.isPaysDropdownOpen;
  }

  /** Met à jour filteredPays selon le terme saisi */
  filterPays(): void {
    const term = this.paysSearchTerm.toLowerCase();
    this.filteredPays = this.paysList.filter(p =>
      p.nom.toLowerCase().includes(term)
    );
  }

  /** Renvoie le nom du pays pour l’ID en param */
  getPaysName(id?: number): string {
    return this.paysList.find(p => p.idPays === id)?.nom || '';
  }

  /** Sélectionne un pays dans la liste */
  selectPays(idPays: number): void {
    this.societeGroup.get('paysId')!.setValue(idPays);
    this.isPaysDropdownOpen = false;
    this.paysSearchTerm = '';
    this.filteredPays = this.paysList;
  }

  selectPaysClient(idPays: number): void {
    this.clientGroup.get('pays')!.setValue(idPays);
    this.isPaysDropdownOpen = false;
    this.paysSearchTerm = '';
    this.filteredPays = this.paysList;
  }
  openPaysModal(): void {
    const modal = this.overlayModalService.open(PaysModalComponent);
    // Quand un nouveau pays est ajouté, on recharge la liste
    modal.added.subscribe(() => {
      this.loadPays();          // recharge paysList & filteredPays
      this.overlayModalService.close();
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.custom-select')) {
      this.isPaysDropdownOpen = false;
    }
  }

  private loadSocietes(): void {
    this.societeService.getSocietes().subscribe(data => this.societesList = data);
  }
  
  // Charger les projets d'une société donnée
  private loadProjetsBySociete(societeId: number): void {
    this.projetService.getProjetsBySocieteId(societeId)
      .subscribe(projs => this.projetsList = projs);
  }

}
