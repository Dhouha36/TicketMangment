import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormGroup, FormControl, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TypeContrat } from '../DTOs/type-contrat.enum';

@Component({
    selector: 'app-contract-dialog',
    imports: [ReactiveFormsModule, CommonModule],
    templateUrl: './contract-dialog.component.html',
    styleUrls: ['./contract-dialog.component.css']
})
export class ContractDialogComponent implements OnInit {

  types = [TypeContrat.CDD, TypeContrat.CDI];
  isProject: boolean;
  
  constructor(
    public dialogRef: MatDialogRef<ContractDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { contractForm: FormGroup, isProject: boolean }
  ) {
    this.isProject = data.isProject;
  }

  ngOnInit(): void {
    // si le FormGroup n'existe pas, on le crée
    if (!this.data.contractForm) {
      this.data.contractForm = new FormGroup({
        dateDebut: new FormControl('', Validators.required),
        dateFin:   new FormControl('', Validators.required),
        type:      new FormControl(
                      this.isProject ? TypeContrat.Projet : null,
                      Validators.required
                    )
      });
    }

    if (this.isProject) {
      // on fixe automatiquement le type sur Projet et on masque le champ
      this.data.contractForm.get('type')!.setValue(TypeContrat.Projet);
    }
  }

  onSave(): void {
    if (this.data.contractForm.valid) {
      this.dialogRef.close(this.data.contractForm.value);
    }
  }

  onClose(): void {
    this.dialogRef.close(false);
  }
}
