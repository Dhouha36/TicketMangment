import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { SocieteService } from '../../_services/societe.service';
import { PaysService } from '../../_services/pays.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { LoaderService } from '../../_services/loader.service';
import { MatStepper, MatStepperModule } from '@angular/material/stepper';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { AjouterProjetComponent } from 'src/app/Projets/ajouter-projet/ajouter-projet.component';
import { AjouterUtilisateurComponent } from 'src/app/utilisateurs/ajouter-utilisateur/ajouter-utilisateur.component';
import { ContractDialogComponent } from 'src/app/contract-dialog/contract-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { Projet } from 'src/app/_models/Projet';
import { User } from 'src/app/_models/user';
import { Societe } from 'src/app/_models/societe';
import { ContractCreate, SocieteCreate } from 'src/app/_models/societe-create.model';

@Component({
    selector: 'app-ajouter-societe',
    imports: [ReactiveFormsModule, CommonModule,
      MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    AjouterProjetComponent,
    AjouterUtilisateurComponent
    ],
    templateUrl: './ajouter-societe.component.html',
    styleUrls: ['./ajouter-societe.component.css']
})
export class AjouterSocieteComponent implements OnInit {
  societeForm!: FormGroup;
  paysList: any[] = [];
  selectedCountry?: any;
  isLoading = false;

  step = 1;
  createdSocieteId!: number;

  constructor(
    private fb: FormBuilder,
    private societeService: SocieteService,
    private paysService: PaysService,
    private loaderService: LoaderService,
    private toastr: ToastrService,
    private router: Router,
    private dialog: MatDialog
  ) {
    this.loaderService.isLoading$.subscribe(loading => this.isLoading = loading);
  }

  ngOnInit(): void {
    this.initForm();
    this.loadPays();

    // Met à jour le préfixe téléphonique
    this.societeForm.get('paysId')!.valueChanges
      .subscribe(id => {
        this.selectedCountry = this.paysList.find(p => p.idPays === +id);
      });

    // Dynamique des validateurs sur le groupe contract
    this.societeForm.get('contrat')!.valueChanges.subscribe(checked => {
      if (checked) {
        this.addContractValidators();
      } else {
        this.clearContractValidators();
      }
    });
  }

  private initForm(): void {
    this.societeForm = this.fb.group({
      nom:       ['', Validators.required],
      adresse:   ['', Validators.required],
      telephone: ['', [Validators.required, Validators.pattern('^[0-9\\s]+$')]],
      paysId:    ['', Validators.required],
      contrat:   [false],
      contract: this.fb.group({
        dateDebut: [''],
        dateFin:   [''],
        type:      ['Standard']
      })
    });
  }

  private loadPays(): void {
    this.paysService.getPays().subscribe({
      next: data => this.paysList = data,
      error: ()   => this.toastr.error('Erreur lors du chargement des pays')
    });
  }

  /** Lorsque l'utilisateur coche la case Contrat, on ouvre la modale */
  onContractChange(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.openContractDialog();
    }
    // Les validateurs sur contract sont gérés par le subscribe valueChanges
  }

  private openContractDialog(): void {
    const dialogRef = this.dialog.open(ContractDialogComponent, {
      width: '400px',
      data: { contractForm: this.societeForm.get('contract') }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) {
        // Annulé dans la modale → on décoche et on retire validateurs
        this.societeForm.get('contrat')!.setValue(false, { emitEvent: false });
        this.clearContractValidators();
      }
    });
  }

  private addContractValidators(): void {
    const cg = this.societeForm.get('contract') as FormGroup;
    cg.get('dateDebut')!.setValidators(Validators.required);
    cg.get('dateFin')!.setValidators(Validators.required);
    cg.get('type')!.setValidators(Validators.required);
    cg.get('dateDebut')!.updateValueAndValidity();
    cg.get('dateFin')!.updateValueAndValidity();
    cg.get('type')!.updateValueAndValidity();
  }

  private clearContractValidators(): void {
    const cg = this.societeForm.get('contract') as FormGroup;
    cg.get('dateDebut')!.clearValidators();
    cg.get('dateFin')!.clearValidators();
    cg.get('type')!.clearValidators();
    cg.get('dateDebut')!.updateValueAndValidity();
    cg.get('dateFin')!.updateValueAndValidity();
    cg.get('type')!.updateValueAndValidity();
    cg.reset({ dateDebut: '', dateFin: '', type: 'Standard' }, { emitEvent: false });
  }

  /** Soumission étape 1 : création de la société */
  submitSociete(): void {
    if (this.societeForm.invalid) { return; }
  
    const fv = this.societeForm.value as any;
    let contractPayload: ContractCreate | null = null;
  
    if (fv.contrat) {
      const toIso = (d: any) => (d instanceof Date ? d : new Date(d)).toISOString();
      contractPayload = {
        dateDebut: toIso(fv.contract.dateDebut),
        dateFin:   toIso(fv.contract.dateFin),
        type:      fv.contract.type
      };
    }
  
    // Construire le DTO de création
    const societeDto: SocieteCreate = {
      nom:       fv.nom,
      adresse:   fv.adresse,
      telephone: fv.telephone,
      paysId:    +fv.paysId,
      contract:  contractPayload
    };
  
    this.loaderService.showLoader();
    this.societeService.addSociete(societeDto).subscribe({
      next: (resp: Societe) => {
        this.toastr.success('Société créée avec succès');
        this.createdSocieteId = resp.id;
        this.step = 2;
        this.loaderService.hideLoader();
      },
      error: () => {
        this.toastr.error('Erreur lors de la création de la société');
        this.loaderService.hideLoader();
      }
    });
  }

  /** Retour à l'étape précédente */
  prevStep(): void {
    this.step = Math.max(this.step - 1, 1);
  }

  /** Handler émis par le composant AjouterProjetComponent */
  onProjetCreated(projet: Projet): void {
    this.toastr.success('Projet créé');
    this.step = 3;
  }

  /** Handler émis par le composant AjouterUtilisateurComponent */
  onUserCreated(user: User): void {
    this.toastr.success('Client ajouté');
    this.router.navigate(['/home/Societes']);
  }
}