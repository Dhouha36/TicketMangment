import { ProjetService } from './../../_services/projet.service';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { ToastrService } from 'ngx-toastr';
import { Pays } from 'src/app/_models/pays';
import { Projet } from 'src/app/_models/Projet';
import { Societe } from 'src/app/_models/societe';
import { ClientService } from 'src/app/_services/client.service';
import { OverlayModalService } from 'src/app/_services/overlay-modal.service';
import { PaysService } from 'src/app/_services/pays.service';
import { SocieteService } from 'src/app/_services/societe.service';
import { ConfirmModalComponent } from 'src/app/confirm-modal/confirm-modal.component';
import { ClientCreate } from 'src/app/DTOs/client-create.model';
import { RegisterClientDto } from 'src/app/DTOs/RegisterClientDto';
import { MatSelectModule } from '@angular/material/select';
import { PaysModalComponent } from 'src/app/PaysFile/pays-modal/pays-modal.component';

@Component({
  selector: 'app-ajouter-client',
  imports: [CommonModule, ReactiveFormsModule, NgSelectModule, MatSelectModule, FormsModule ],
  templateUrl: './ajouter-client.component.html',
  styleUrl: './ajouter-client.component.css'
})
export class AjouterClientComponent implements OnInit {
  clientForm: FormGroup;
  paysList: Pays[] = [];
  projetsList: Projet[] = [];
  selectedCountry?: Pays;   
  societesList: Societe[] = [];
  isLoading = false;
  isLoadingProjets = false;

  filteredPays: Pays[] = [];
  paysSearchTerm = '';
  isPaysDropdownOpen = false;

  constructor(
    private fb: FormBuilder,
    private paysService: PaysService,
    private projetService: ProjetService,
    private societeService: SocieteService,
    private clientService: ClientService,
    private router: Router,
    private toastr: ToastrService,
    private overlayModalService: OverlayModalService
  ) {
    this.clientForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      pays: ['', Validators.required],
      numTelephone: ['', [Validators.required, Validators.pattern(/^[0-9\s]+$/)]],
      role: ['Client'],
      societeId: ['', Validators.required],
      actif:[true],
      projetIds: [[], [Validators.required, Validators.minLength(1)]]
    });
  }

  ngOnInit(): void {
    this.loadPays();
    this.loadSocietes();

    // Met à jour selectedCountry dès que "pays" change
    this.clientForm.get('pays')!.valueChanges.subscribe(id => {
      this.selectedCountry = this.paysList.find(p => p.idPays === +id);
      // réinitialiser le numéro si vous le souhaitez
      this.clientForm.get('numTelephone')!.reset();
    });

    this.clientForm.get('societeId')!.valueChanges.subscribe(id => {
      this.clientForm.patchValue({ projetIds: [] }); // reset sélection
      if (id) this.loadProjetsBySociete(id);
      else this.projetsList = [];
    });
  }

  private loadPays(): void {
    this.paysService.getPays().subscribe({
      next: data => {
        this.paysList     = data;
        this.filteredPays = data;
      },
      error: () => this.toastr.error('Erreur chargement pays')
    });
  }

  getPaysName(id?: number): string {
    return this.paysList.find(p => p.idPays === id)?.nom || '';
  }

  togglePaysDropdown(): void {
    this.isPaysDropdownOpen = !this.isPaysDropdownOpen;
  }

  filterPays(): void {
    const term = this.paysSearchTerm.toLowerCase();
    this.filteredPays = this.paysList.filter(p =>
      p.nom.toLowerCase().includes(term)
    );
  }

  selectPays(idPays: number): void {
    this.clientForm.get('pays')!.setValue(idPays);
    this.isPaysDropdownOpen = false;
    this.paysSearchTerm = '';
    this.filteredPays = this.paysList;
  }

  openPaysModal(): void {
    const modal = this.overlayModalService.open(PaysModalComponent);
    modal.added.subscribe(() => {
      this.paysService.getPays().subscribe(data => {
        this.paysList     = data;
        this.filteredPays = data;
      });
      this.overlayModalService.close();
    });
  }

  private loadSocietes(): void {
    this.societeService.getSocietes().subscribe({ next: data => this.societesList = data });
  }

  private loadProjetsBySociete(societeId: number): void {
    this.isLoadingProjets = true;
    this.projetService.getProjetsBySocieteId(societeId)
      .subscribe({
        next: data => {
          this.projetsList = data;
          this.isLoadingProjets = false;
        },
        error: () => {
          this.toastr.error('Erreur chargement projets');
          this.isLoadingProjets = false;
        }
      });
  }

  onSubmit(): void {
    if (this.clientForm.valid) {
      this.isLoading = true;
      const dto: RegisterClientDto = this.clientForm.value;
      this.clientService.register(dto).subscribe({
        next: client => {
          this.toastr.success(
            `Client créé avec succès. Mot de passe initial : ${client.initialPassword}`
          );
          this.router.navigate(['/home/clients']);
        },
        error: err => {
          this.toastr.error(err.error?.message || 'Erreur lors de la création');
          this.isLoading = false;
        }
      });
    }
  }
  

  onCancel(): void {
    if (this.clientForm.dirty) {
      const modal = this.overlayModalService.open(ConfirmModalComponent);
      modal.message = 'Annuler la création du client ?';
      modal.confirmed.subscribe(() => this.router.navigate(['/home/clients']));
      modal.cancelled.subscribe(() => this.overlayModalService.close());
    } else {
      this.router.navigate(['/home/clients']);
    }
  }

  trackByProjetId(index: number, proj: Projet): number {
    return proj.id;
  }
  
}

