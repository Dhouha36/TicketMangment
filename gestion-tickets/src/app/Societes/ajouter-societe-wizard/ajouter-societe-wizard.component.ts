import { CommonModule, NgIf } from '@angular/common';
import { Component, HostListener, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Pays } from 'src/app/_models/pays';
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

@Component({
  selector: 'app-ajouter-societe-wizard',
  imports: [ReactiveFormsModule, CommonModule, FormsModule],
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
  createdSocieteId!: number;
  createdProjetId!: number;

  filteredPays: Pays[] = [];
  paysSearchTerm = '';
  isPaysDropdownOpen = false;

  constructor(
    private fb: FormBuilder,
    private societeService: SocieteService,
    private projetService: ProjetService,
    private accountService: AccountService,
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
    this.filteredPays = this.paysList;

    // Met à jour le préfixe téléphonique en fonction du pays sélectionné
    this.wizardForm.get('societe.paysId')!.valueChanges
      .subscribe(id => {
        this.selectedCountry = this.paysList.find(p => p.idPays === +id);
      });

    // Ouvre modale et pose validateurs quand on coche “contrat”
    this.wizardForm.get('societe.contrat')!.valueChanges
      .subscribe(checked => {
        if (checked) {
          this.openContractDialog();
          this.addContractValidators();
        } else {
          this.clearContractValidators();
        }
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
        contrat: [false],
        contract: this.fb.group({
          dateDebut: [''],
          dateFin: [''],
          type: ['Standard']
        })
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
        pays: [null, Validators.required],
        numTelephone: ['', [
          Validators.required,
          Validators.pattern('^[0-9\\s]+$'),
          Validators.minLength(8),
          Validators.maxLength(15)
        ]]
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
        error: ()   => this.toastr.error('Erreur chargement chefs de projet')
      });
  }

  private openContractDialog(): void {
    const dialogRef = this.dialog.open(ContractDialogComponent, {
      data: { contractForm: this.wizardForm.get('societe.contract') }
    });
    dialogRef.afterClosed().subscribe(result => {
      if (!result) {
        this.wizardForm.get('societe.contrat')!.setValue(false, { emitEvent: false });
        this.clearContractValidators();
      }
    });
  }

  private addContractValidators(): void {
    const cg = this.wizardForm.get('societe.contract') as FormGroup;
    cg.get('dateDebut')!.setValidators(Validators.required);
    cg.get('dateFin')!.setValidators(Validators.required);
    cg.get('type')!.setValidators(Validators.required);
    cg.updateValueAndValidity();
  }

  private clearContractValidators(): void {
    const cg = this.wizardForm.get('societe.contract') as FormGroup;
    cg.get('dateDebut')!.clearValidators();
    cg.get('dateFin')!.clearValidators();
    cg.get('type')!.clearValidators();
    cg.reset({ dateDebut: '', dateFin: '', type: 'Standard' }, { emitEvent: false });
  }



  submitAll(): void {
    // 1) Vérification du dernier formulaire
    if (this.clientGroup.invalid) {
      this.toastr.error('Veuillez compléter tous les champs requis avant de continuer.');
      return;
    }
  
    this.loaderService.showLoader();
  
    // 2) Préparation et appel API pour la création de la société
    const soc = this.societeGroup.value;
    const socDto = {
      nom: soc.nom,
      adresse: soc.adresse,
      telephone: `${this.selectedCountry?.codeTel || ''} ${soc.telephone}`.trim(),
      paysId: +soc.paysId,
      contract: soc.contrat ? {
        dateDebut: new Date(soc.contract.dateDebut).toISOString(),
        dateFin:   new Date(soc.contract.dateFin).toISOString(),
        type:      soc.contract.type
      } : null
    };
  
    this.societeService.addSociete(socDto).subscribe({
      next: socResp => {
        const societeId = socResp.id;
  
        // 3) Préparation et appel API pour la création du projet
        const p = this.projetGroup.value;
        const projDto = {
          nom: p.nom,
          description: p.description,
          chefProjetId: +p.chefProjetId,
          societeId
        };
  
        this.projetService.addProjet(projDto).subscribe({
          next: projResp => {
            const projetId = projResp.id;
  
            // 4) Préparation et appel API pour la création du client
            const c = this.clientGroup.value;
            const clientDto: ClientCreate = {
              firstName: c.firstName,
              lastName:  c.lastName,
              email:     c.email,
              pays:      +c.pays,
              numTelephone: this.selectedCountry!.codeTel + ' ' + c.numTelephone,
              role:      'Client',
              societeId,
              projetId   // Assurez-vous d’avoir ajouté `projetId` dans ClientCreate
            };
  
            this.accountService.register(clientDto).subscribe({
              next: () => {
                this.toastr.success('Société, projet et client créés avec succès.');
                this.router.navigate(['/home/Societes']);
              },
              error: () => {
                this.toastr.error('Erreur lors de la création du client.');
              },
              complete: () => {
                this.loaderService.hideLoader();
                this.isCompleted = true;
              }
            });
  
          },
          error: () => {
            this.toastr.error('Erreur lors de la création du projet.');
            this.loaderService.hideLoader();
          }
        });
  
      },
      error: () => {
        this.toastr.error('Erreur lors de la création de la société.');
        this.loaderService.hideLoader();
      }
    });
  }
  
  prevStep(): void {
    this.step = Math.max(this.step - 1, 1);
  }
  nextStep(): void {
    // n’avance que si le formulaire courant est valide
    if (
      (this.step === 1 && this.societeGroup.invalid) ||
      (this.step === 2 && this.projetGroup.invalid)
    ) { return; }
    this.step++;
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
      next:     () => { /* OK, la société est supprimée */ },
      error:    () => { /* On continue quand même */ }
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


}
