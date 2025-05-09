import { CommonModule, NgIf } from '@angular/common';
import { Component, HostListener, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Projet } from 'src/app/_models/Projet';
import { Societe } from 'src/app/_models/societe';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';
import { LoaderService } from 'src/app/_services/loader.service';
import { PaysService } from 'src/app/_services/pays.service';
import { ProjetService } from 'src/app/_services/projet.service';
import { SocieteService } from 'src/app/_services/societe.service';
import { ContractDialogComponent } from 'src/app/contract-dialog/contract-dialog.component';
import { ClientCreate } from 'src/app/DTOs/client-create.model';

@Component({
  selector: 'app-ajouter-societe-wizard',
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './ajouter-societe-wizard.component.html',
  styleUrl: './ajouter-societe-wizard.component.css'
})
export class AjouterSocieteWizardComponent implements OnInit {
  wizardForm!: FormGroup;
  step = 1;
  isLoading = false;
  paysList: any[] = [];
  chefsList: User[] = [];
  selectedCountry?: any;
  createdSocieteId!: number;
  createdProjetId!: number;

  constructor(
    private fb: FormBuilder,
    private societeService: SocieteService,
    private projetService: ProjetService,
    private accountService: AccountService,
    private paysService: PaysService,
    private loaderService: LoaderService,
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
      next: data => this.paysList = data,
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

  // Étape 1 → création société
  submitSociete(): void {
    const socForm = this.societeGroup.value;
    if (this.societeGroup.invalid) return;

    // Prépare payload commun
    const payload = {
      nom: socForm.nom,
      adresse: socForm.adresse,
      telephone: `${this.selectedCountry?.codeTel || ''} ${socForm.telephone}`.trim(),
      paysId: +socForm.paysId,
      contract: socForm.contrat ? {
        dateDebut: new Date(socForm.contract.dateDebut).toISOString(),
        dateFin:   new Date(socForm.contract.dateFin).toISOString(),
        type:      socForm.contract.type
      } : null
    };

    this.isLoading = true;

    if (this.createdSocieteId) {
      // On récupère l'entité complète, on patch, puis on update
      this.societeService.getSociete(this.createdSocieteId).subscribe(existing => {
        const toUpdate: Societe = {
          ...existing,
          ...payload,
          utilisateurs: existing.utilisateurs || [],
          projets: existing.projets || []
        };
        this.societeService.updateSociete(this.createdSocieteId!, toUpdate).subscribe({
          next: () => console.log('Société mise à jour'),
          complete: () => { this.isLoading = false; this.step = 2; }
        });
      });
    } else {
      // Création
      this.societeService.addSociete(payload).subscribe({
        next: resp => { this.createdSocieteId = resp.id; this.step = 2; },
        error: () => this.toastr.error('Erreur création société'),
        complete: () => this.isLoading = false
      });
    }
  }

  /** Création ou mise à jour du projet */
  submitProjet(): void {
    const projForm = this.projetGroup.value;
    if (this.projetGroup.invalid) return;

    const payload = {
      nom: projForm.nom,
      description: projForm.description,
      chefProjetId: +projForm.chefProjetId,
      societeId: this.createdSocieteId!
    };

    this.isLoading = true;

    if (this.createdProjetId) {
      // patch existing project
      this.projetService.getProjetById(this.createdProjetId).subscribe(existing => {
        const toUpdate: Projet = {
          ...existing,
          ...payload
        };
        this.projetService.updateProjet(toUpdate).subscribe({
          next: () => console.log('Projet mis à jour'),
          complete: () => { this.isLoading = false; this.step = 3; }
        });
      });
    } else {
      // création
      this.projetService.addProjet(payload).subscribe({
        next: resp => { this.createdProjetId = resp.id; this.step = 3; },
        error: () => this.toastr.error('Erreur création projet'),
        complete: () => this.isLoading = false
      });
    }
  }

  // Étape 3 → création client
  submitClient(): void {
    if (this.wizardForm.get('client')!.invalid) return;

    const f = this.wizardForm.get('client')!.value;
    const clientDto: ClientCreate = {
      firstName: f.firstName,
      lastName: f.lastName,
      email: f.email,
      pays: +f.pays,
      numTelephone: this.selectedCountry!.codeTel + ' ' + f.numTelephone,
      role: 'Client',
      societeId: this.createdSocieteId
    };

    this.loaderService.showLoader();
    this.accountService.register(clientDto).subscribe({
      next: () => {
        this.toastr.success('Ajout terminé avec succès');
        this.router.navigate(['/home/Societes']);
        this.loaderService.hideLoader();
      },
      error: () => {
        this.toastr.error('Erreur création client');
        this.loaderService.hideLoader();
      }
    });
  }

  prevStep(): void {
    this.step = Math.max(this.step - 1, 1);
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


}
