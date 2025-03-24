import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { SocieteService } from '../../_services/societe.service';
import { PaysService } from '../../_services/pays.service';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { CommonModule } from '@angular/common';
import { ContractDialogComponent } from '../../contract-dialog/contract-dialog.component';
import { MatDialog } from '@angular/material/dialog';

@Component({
    selector: 'app-ajouter-societe',
    imports: [ReactiveFormsModule, CommonModule],
    templateUrl: './ajouter-societe.component.html',
    styleUrls: ['./ajouter-societe.component.scss']
})
export class AjouterSocieteComponent implements OnInit {
  societeForm!: FormGroup;
  paysList: any[] = []; // Liste des pays
  societesList: any[] = []; // Liste des sociétés

  constructor(
    private fb: FormBuilder,
    private societeService: SocieteService,
    private paysService: PaysService,
    private router: Router,
    private toastr: ToastrService,
    private dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    // Création du formulaire incluant le groupe pour le contrat (optionnel)
    this.societeForm = this.fb.group({
      nom: ['', Validators.required],
      adresse: ['', Validators.required],
      telephone: ['', [Validators.required, Validators.pattern('^[+0-9\\-\\s]+$')]],
      paysId: ['', Validators.required],
      contrat: [false],  // Checkbox pour activer l'ajout du contrat
      // Groupe pour les informations du contrat (initialisé sans validateurs)
      contract: this.fb.group({
        dateDebut: [''],
        dateFin: [''],
        type: ['Standard']
      })
    });

    // Charger la liste des pays et des sociétés
    this.loadPays();
    this.loadSocietes();

    // Au démarrage, si la case "contrat" est désactivée, on s'assure que le groupe de contrat n'impose aucun validateur
    if (!this.societeForm.get('contrat')?.value) {
      this.clearContractValidators();
    }

    // Mise à jour des validateurs en fonction de la case "contrat"
    this.societeForm.get('contrat')?.valueChanges.subscribe(checked => {
      if (checked) {
        // Ajout des validateurs si la case est cochée
        this.societeForm.get('contract.dateDebut')?.setValidators(Validators.required);
        this.societeForm.get('contract.dateFin')?.setValidators(Validators.required);
        this.societeForm.get('contract.type')?.setValidators(Validators.required);
      } else {
        // Suppression des validateurs si la case n'est pas cochée
        this.clearContractValidators();
      }
      // Mise à jour de la validité des champs du groupe contract
      this.societeForm.get('contract.dateDebut')?.updateValueAndValidity();
      this.societeForm.get('contract.dateFin')?.updateValueAndValidity();
      this.societeForm.get('contract.type')?.updateValueAndValidity();
    });
  }

  clearContractValidators(): void {
    this.societeForm.get('contract.dateDebut')?.clearValidators();
    this.societeForm.get('contract.dateFin')?.clearValidators();
    this.societeForm.get('contract.type')?.clearValidators();
  }

  loadPays(): void {
    this.paysService.getPays().subscribe({
      next: (data) => { this.paysList = data; },
      error: (err) => { console.error('Erreur lors du chargement des pays', err); }
    });
  }

  loadSocietes(): void {
    this.societeService.getSocietes().subscribe({
      next: (data) => { this.societesList = data; },
      error: (err) => { console.error('Erreur lors du chargement des sociétés', err); }
    });
  }

  onSubmit(): void {
    if (this.societeForm.valid) {
      const formValue = this.societeForm.value;
      const societeForAdd: any = {
        nom: formValue.nom,
        adresse: formValue.adresse,
        telephone: formValue.telephone,
        paysId: +formValue.paysId
      };

      if (formValue.contrat) {
        societeForAdd.contract = {
          dateDebut: formValue.contract.dateDebut,
          dateFin: formValue.contract.dateFin,
          type: formValue.contract.type
        };
      } else {
        societeForAdd.contract = null;
      }
      this.societeService.addSociete(societeForAdd).subscribe({
        next: () => {
          this.toastr.success("Ajouté avec succès");
          this.router.navigate(['/home/Societes']);
        },
        error: (error) => {
          console.error('Erreur ajout projet', error);
          let errMsg = "Erreur lors de l'ajout de la société.";
          if (Array.isArray(error)) {
            errMsg = error.join(' ');
          } else if (typeof error === 'string') {
            errMsg = error;
          } else if (error.error) {
            if (Array.isArray(error.error)) {
              errMsg = error.error.join(' ');
            } else if (typeof error.error === 'string') {
              errMsg = error.error;
            } else if (typeof error.error === 'object') {
              errMsg = error.error.message || JSON.stringify(error.error);
            }
          } else if (error.message) {
            errMsg = error.message;
          }
          this.toastr.error(errMsg);
        }
      });
    }
  }
  
  onContractChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.checked) {
      this.openContractDialog();
    }
  }

  openContractDialog(): void {
    const dialogRef = this.dialog.open(ContractDialogComponent, {
      data: { 
        contractForm: this.societeForm.get('contract')
      }
    });
  
    dialogRef.afterClosed().subscribe(result => {
      if (!result) {
        this.societeForm.get('contrat')?.setValue(false);
      }
    });
  }
  
  onCancel(): void {
    this.router.navigate(['/home/Societes']);
  }
}
