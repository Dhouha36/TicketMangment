import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { ProjetService } from '../../_services/projet.service';
import { SocieteService } from '../../_services/societe.service';
import { PaysService } from '../../_services/pays.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, NgIf } from '@angular/common';
import { AccountService } from '../../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { MatSidenavModule } from '@angular/material/sidenav';
import { LoaderService } from '../../_services/loader.service';
import { ProjetCreate } from 'src/app/DTOs/projet-create.model';
import { RegisterClientDto } from 'src/app/DTOs/RegisterClientDto';
import { ClientService } from 'src/app/_services/client.service';
import { catchError, concatMap } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { ContractDialogComponent } from 'src/app/contract-dialog/contract-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { TypeContrat } from 'src/app/DTOs/type-contrat.enum';
import { PendingChangesComponent } from 'src/app/_guards/pending-changes.guard';
import { OverlayModalService } from 'src/app/_services/overlay-modal.service';
import { PaysModalComponent } from 'src/app/PaysFile/pays-modal/pays-modal.component';

@Component({
  selector: 'app-ajouter-projet',
  imports: [CommonModule, FormsModule, NgIf, ReactiveFormsModule, MatSidenavModule],
  templateUrl: './ajouter-projet.component.html',
  styleUrls: ['./ajouter-projet.component.css']
})
export class AjouterProjetComponent implements OnInit, PendingChangesComponent {
  wizardForm!: FormGroup;
  step = 1;
  clientMode: 'existant' | 'nouveau' | null = null;
  isLoading = false;
  isCompleted = false;

  societes = [] as any[];
  chefsProjet = [] as any[];
  paysList = [] as any[];
  selectedCountry?: any;
  existingClients: any[] = [];

  serverErrors = {
    projet: '' as string,
    client: '' as string,
  };

  filteredPays: any[] = [];
  paysSearchTerm = '';
  isPaysDropdownOpen = false;

  constructor(
    private fb: FormBuilder,
    private projetService: ProjetService,
    private societeService: SocieteService,
    private overlayModalService: OverlayModalService,
    private accountService: AccountService,
    private dialog: MatDialog,
    private clientService: ClientService,
    private paysService: PaysService,
    private toastr: ToastrService,
    private loaderService: LoaderService,
    private router: Router
  ) {
    this.loaderService.isLoading$.subscribe(l => this.isLoading = l);
  }

  ngOnInit(): void {
    this.initForm();
    this.loadSocietes();
    this.loadChefs();
    this.loadPays();

    this.projetGroup.valueChanges.subscribe(val => {
      if (this.step === 2) {
        const societeId = this.projetGroup.value.societeId;
        this.clientService.getBySociete(societeId).subscribe(
          clients => this.existingClients = clients,
          () => this.toastr.error('Impossible de charger les clients existants')
        );
      }
    });
    this.clientGroup.get('modeClient')!.valueChanges
      .subscribe(val => this.clientMode = val);

    this.clientGroup.get('pays')!.valueChanges.subscribe(id => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +id);
    });
  }

  cleanupOnExit(): void {
    // Réinitialise le wizard ou masque une éventuelle modale en cours
    this.step = 1;
    this.wizardForm.reset();
  }

  private initForm(): void {
    // 1) Création du FormGroup projet
    const projetGroup = this.fb.group({
      nom: ['', Validators.required],
      societeId: [null, Validators.required],
      chefProjetId: [null, Validators.required],

      // toggle obligatoire
      hasContract: [false, Validators.requiredTrue],
      contratProjet: this.fb.group({
        dateDebut: ['', Validators.required],
        dateFin: ['', Validators.required],
        montantTotal: [null]
      })
    });

    // 2) Désactivation initiale du sous-groupe contract
    const contractGroup = projetGroup.get('contratProjet') as FormGroup;
    contractGroup.disable({ emitEvent: false });

    // 3) Réaction au changement de hasContract
    projetGroup.get('hasContract')!.valueChanges.subscribe(checked => {
      if (checked) {
        // on active les champs de la modal et on l’ouvre
        contractGroup.enable();
        this.openContractDialogForProject();
      } else {
        // on vide et désactive le sous-groupe
        contractGroup.reset({ dateDebut: '', dateFin: '', montantTotal: null });
        contractGroup.disable();
      }
    });

    // 4) Création du FormGroup client (reste inchangé)
    const clientGroup = this.fb.group({
      modeClient: ['existant', Validators.required],
      clientExistantId: [null],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      pays: [null, Validators.required],
      actif: [true],
      numTelephone: ['', [
        Validators.required,
        Validators.pattern('^[0-9\\s]+$'),
        Validators.minLength(8),
        Validators.maxLength(15)
      ]]
    });

    // 5) Ajustements dynamiques du FormGroup client (tel que déjà en place)
    clientGroup.get('modeClient')!.valueChanges.subscribe(mode => {
      const existCtrl = clientGroup.get('clientExistantId')!;
      const newCtrls = ['firstName', 'lastName', 'email', 'pays', 'numTelephone'];
      if (mode === 'existant') {
        existCtrl.setValidators([Validators.required]);
        newCtrls.forEach(f => clientGroup.get(f)!.disable());
      } else {
        existCtrl.clearValidators();
        newCtrls.forEach(f => clientGroup.get(f)!.enable());
      }
      existCtrl.updateValueAndValidity();
    });

    // 6) Assemblage final
    this.wizardForm = this.fb.group({
      projet: projetGroup,
      client: clientGroup
    });
  }


  private loadSocietes() {
    this.societeService.getSocietes().subscribe(
      data => this.societes = data,
      () => this.toastr.error('Erreur chargement sociétés')
    );
  }
  private loadChefs() {
    this.accountService.getUsersByRole('Chef de Projet').subscribe(
      users => this.chefsProjet = users,
      () => this.toastr.error('Erreur chargement chefs de projet')
    );
  }
  private loadPays() {
    this.paysService.getPays().subscribe({
      next: data => {
        this.paysList     = data;
        this.filteredPays = data;
      },
      error: ()=> this.toastr.error('Erreur chargement pays')
    });    
  }

  get projetGroup() { return this.wizardForm.get('projet') as FormGroup; }
  get clientGroup() { return this.wizardForm.get('client') as FormGroup; }

  nextStep(): void {
    // Réinitialise les erreurs serveur
    this.serverErrors = { projet: '', client: '' };

    if (this.step === 1) {
      // Étape 1 : validation du projet
      if (this.projetGroup.invalid) {
        this.projetGroup.markAllAsTouched();
        return;
      }

      const dto: ProjetCreate = { ...this.projetGroup.value };
      this.projetService.validateProjet(dto).subscribe({
        next: () => {
          // Passe à l'étape 2 : choix du type de client
          this.step = 2;

          // Précharge les clients existants pour la société sélectionnée
          const societeId = this.projetGroup.value.societeId;
          this.clientService.getBySociete(societeId).subscribe(
            clients => this.existingClients = clients,
            () => this.toastr.error('Impossible de charger les clients existants')
          );
        },
        error: err => {
          const msg = typeof err === 'string' ? err : err.error?.message || err.message;
          this.serverErrors.projet = msg || 'Erreur de validation du projet';
          this.loaderService.hideLoader();
        }
      });

    } else if (this.step === 2) {
      if (!this.clientMode) {
        this.toastr.warning('Veuillez sélectionner le type de client');
        return;
      }
      this.step = 3;
    }
    else if (this.step === 3) {
      // Si jamais on clique « Suivant » en step 3, on lance la soumission finale
      if (this.clientGroup.invalid) {
        this.clientGroup.markAllAsTouched();
        return;
      }
      this.submitAll();
    }
  }


  prevStep() {
    if (this.step > 1) {
      this.step--;
    }
  }

  submitAll(): void {
    if (this.clientGroup.invalid) return;

    this.serverErrors = { projet: '', client: '' };
    this.loaderService.showLoader();

    // Récupération des valeurs du projet
    const pg = this.projetGroup.value;
    const projetDto: ProjetCreate = {
      nom: pg.nom,
      description: '',
      societeId: pg.societeId,
      chefProjetId: pg.chefProjetId,
      // n’inclure que si hasContract est vrai
      contratProjet: pg.hasContract ? {
        dateDebut: pg.contratProjet.dateDebut,
        dateFin: pg.contratProjet.dateFin,
        montantTotal: pg.contratProjet.montantTotal
      } : undefined
    };

    this.projetService.addProjet(projetDto).pipe(
      concatMap(projResp => {
        const projetId = projResp.id;
        const mode = this.clientGroup.value.modeClient;

        if (mode === 'existant') {
          const clientId = this.clientGroup.value.clientExistantId;
          return this.clientService.addClientToProject(clientId, projetId)
            .pipe(catchError(err => throwError(() => err)));
        } else {
          const form = this.clientGroup.value;
          const clientDto: RegisterClientDto = {
            email: form.email,
            firstName: form.firstName,
            lastName: form.lastName,
            numTelephone: `${this.selectedCountry!.codeTel} ${form.numTelephone.trim()}`,
            pays: +form.pays,
            societeId: projetDto.societeId,
            actif: form.actif,
            projetIds: [projetId]
          };
          return this.clientService.register(clientDto).pipe(
            catchError(err =>
              this.projetService.deleteProjet(projetId).pipe(
                concatMap(() => throwError(() => err))
              )
            )
          );
        }
      })
    ).subscribe({
      next: () => {
        this.isCompleted = true;
        this.toastr.success('Projet et client créés avec succès');
        this.loaderService.hideLoader();
        this.router.navigate(['/home/Projets']);
      },
      error: (err: any) => {
        const msg = typeof err === 'string'
          ? err
          : (err.error?.message || err.error || err.message || 'Échec de la création');
        this.serverErrors.client = msg;
        this.loaderService.hideLoader();
      }
    });
  }

  onContractChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.checked) {
      this.openContractDialogForProject();
    }
  }

  openContractDialogForProject(): void {
    // passez directement le FormGroup existing
    const dialogRef = this.dialog.open(ContractDialogComponent, {
      data: {
        contractForm: this.projetGroup.get('contratProjet'),
        isProject: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        // result contient { dateDebut, dateFin, type }
        // on met à jour le FormGroup (ça le rend valide)
        this.projetGroup.get('contratProjet')!.setValue(result);
        // on peut marquer comme touched pour montrer les erreurs éventuelles
        this.projetGroup.get('contratProjet')!.markAllAsTouched();
      }
    });
  }

  getPaysName(id?: number): string {
    return this.paysList.find(p => p.idPays === id)?.nom || '';
  }
  
  // Ouvre / ferme la dropdown
  togglePaysDropdown(): void {
    this.isPaysDropdownOpen = !this.isPaysDropdownOpen;
  }
  
  // Filtre la liste en fonction du terme saisi
  filterPays(): void {
    const term = this.paysSearchTerm.toLowerCase();
    this.filteredPays = this.paysList.filter(p =>
      p.nom.toLowerCase().includes(term)
    );
  }
  
  // Quand on clique sur un pays
  selectPaysClient(idPays: number): void {
    this.clientGroup.get('pays')!.setValue(idPays);
    this.isPaysDropdownOpen = false;
    this.paysSearchTerm = '';
    this.filteredPays = this.paysList;
  }
  
  // Ouvre le modal “Autre…”
  openPaysModal(): void {
    const modal = this.overlayModalService.open(PaysModalComponent);
    modal.added.subscribe(() => {
      // on recharge la liste des pays
      this.paysService.getPays().subscribe(data => {
        this.paysList      = data;
        this.filteredPays  = data;
      });
      this.overlayModalService.close();
    });
  }

  @HostListener('window:beforeunload', ['$event'])
  beforeUnloadHandler(event: BeforeUnloadEvent): void {
    if (!this.isCompleted && this.step > 1) {
      event.preventDefault();
      event.returnValue = 'Vous allez perdre les données saisies. Voulez-vous vraiment quitter ?';
    }
  }
}
