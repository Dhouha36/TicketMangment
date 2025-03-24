import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { AjouterProjetComponent } from '../../Projets/ajouter-projet/ajouter-projet.component';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Projet } from '../../_models/Projet';
import { Societe } from '../../_models/societe';
import { Pays } from '../../_models/pays';
import { User } from '../../_models/user';
import { ProjetService } from '../../_services/projet.service';
import { SocieteService } from '../../_services/societe.service';
import { AccountService } from '../../_services/account.service';
import { PaysService } from '../../_services/pays.service';
import { ActivatedRoute, Router } from '@angular/router';
import { DropdownService } from '../../_services/dropdown.service';
import { ToastrService } from 'ngx-toastr';
import { forkJoin } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-project-modal',
  imports: [ ReactiveFormsModule, FormsModule, CommonModule],
  templateUrl: './project-modal.component.html',
  styleUrl: './project-modal.component.scss'
})
export class ProjectModalComponent {
  @Input() societeId!: number; // Reçu depuis le parent
  @Output() closed = new EventEmitter<any>();
  projetForm!: FormGroup;

  projet: Projet = {
    id: 0,
    nom: '',
    description: '',
    societeId: undefined,
    clientId: undefined,
    idPays: 0
  };

  societes: Societe[] = [];
  pays: Pays[] = [];
  utilisateurs: User[] = [];
  chefsProjet: User[] = [];
  clients: User[] = [];

  isPaysDropdownOpen = false;
  isSocieteDropdownOpen = false;
  isChefDropdownOpen = false;
  isClientDropdownOpen = false;

  searchClient = '';
  searchPays = '';
  searchSociete = '';
  searchChef = '';

  filteredPays: Pays[] = [];
  filteredSocietes: Societe[] = [];
  filteredChefs: User[] = [];
  filteredClients: User[] = [];

  // Par défaut, le projet est de type Société
  isSocieteProjet: boolean = true;

  constructor(
    private fb: FormBuilder,
    private projetService: ProjetService,
    private societeService: SocieteService,
    private paysService: PaysService,
    private userService: AccountService,
    private router: Router,
    private dropdownService: DropdownService,
    public route: ActivatedRoute,
    private overlayModalService: OverlayModalService,
    private toastr: ToastrService
  ) { }

  ngOnInit(): void {
    this.initForm();
    this.loadSocietes();
    this.loadPays();
    this.loadUtilisateurs();
  }

  initForm(): void {
    this.projetForm = this.fb.group({
      nom: ['', Validators.required],
      description: [''],
      societeId: [this.societeId, Validators.required],
      chefProjetId: [null, Validators.required],
      idPays: [0]
    });
  }

  loadSocietes(): void {
    this.societeService.getSocietes().subscribe(
      data => { this.societes = data; },
      error => {
        console.error('Erreur chargement sociétés', error);
        this.toastr.error("Erreur lors du chargement des sociétés");
      }
    );
  }

  loadPays(): void {
    this.paysService.getPays().subscribe(
      data => { this.pays = data; },
      error => {
        console.error('Erreur chargement pays', error);
        this.toastr.error("Erreur lors du chargement des pays");
      }
    );
  }

  loadUtilisateurs(): void {
    this.userService.getAllUsers().subscribe(
      data => {
        this.utilisateurs = data;
        this.chefsProjet = data.filter(user => user.role.toLowerCase().trim() === 'chef de projet');
        this.clients = data.filter(user => user.role.toLowerCase().trim() === 'client');
      },
      error => {
        console.error('Erreur chargement utilisateurs', error);
        this.toastr.error("Erreur lors du chargement des utilisateurs");
      }
    );
  }

  ajouterProjet(): void {
    if (this.projetForm.invalid) {
      // Forcer la mise à jour de la validation pour tous les contrôles
      this.projetForm.updateValueAndValidity();
      this.toastr.error("Veuillez remplir tous les champs obligatoires.");
      return;
    }
  
    const formValue = this.projetForm.value;
    this.projet.nom = formValue.nom;
    this.projet.description = formValue.description;
    this.projet.societeId = formValue.societeId;
    this.projet.idPays = +formValue.idPays;
    // ASSIGNATION MANQUANTE : affecter chefProjetId au projet
    this.projet.chefProjetId = formValue.chefProjetId;
  
    // Appel au service pour ajouter le projet
    this.projetService.addProjet(this.projet).subscribe({
      next: (projetCree) => {
        this.toastr.success('Projet créé avec succès');
        this.router.navigate(['/home/Projets']);
      },
      error: (error) => {
        console.error('Erreur ajout projet', error);
        let errMsg = "Erreur lors de l'ajout du projet.";
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


  

  toggleDropdown(type?: string): void {
    if (type) {
      switch (type) {
        case 'chef':
          this.isChefDropdownOpen = !this.isChefDropdownOpen;
          if (this.isChefDropdownOpen) {
            this.filteredChefs = [...this.chefsProjet];
          }
          break;
      }
    }
  }

  filterItems(type: string): void {
    switch (type) {
      case 'chef':
        this.filteredChefs = this.chefsProjet.filter(c =>
          `${c.firstName} ${c.lastName}`.toLowerCase().includes(this.searchChef.toLowerCase())
        );
        break;
    }
  }

  selectItem(item: any, type: string): void {
    switch (type) {
      case 'societe':
        this.projetForm.get('societeId')?.setValue(item.id);
        const selectedSociete = this.societes.find(s => s.id === item.id);
        if (selectedSociete) {
          this.projetForm.patchValue({ idPays: selectedSociete.paysId });
        }
        this.projetForm.get('societeId')?.markAsTouched();
        this.projetForm.get('societeId')?.updateValueAndValidity();
        break;
      case 'chef':
        this.projetForm.get('chefProjetId')?.setValue(item.id);
        this.projetForm.get('chefProjetId')?.markAsTouched();
        this.projetForm.get('chefProjetId')?.updateValueAndValidity();
        break;
    }
    // Ferme les dropdowns après sélection
    this.closeAllDropdowns();
  }

  getPaysName(idPays: number): string {
    return this.pays.find(p => p.idPays === idPays)?.nom || '';
  }

  getSocieteName(idSociete: number | null | undefined): string {
    if (idSociete == null) return '';
    return this.societes.find(s => s.id === idSociete)?.nom || '';
  }

  getChefName(idChef: number | null): string {
    if (!idChef) return '';
    const chef = this.chefsProjet.find(c => c.id === idChef);
    return chef ? `${chef.firstName} ${chef.lastName}` : '';
  }

  getClientName(clientId: number | null | undefined): string {
    if (clientId == null) return '';
    const client = this.clients.find(c => c.id === clientId);
    return client ? `${client.firstName} ${client.lastName}` : '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    // On ajoute '.search-box' à la liste pour ne pas fermer le dropdown lors d'un clic dans la zone de recherche
    const isClickInside = [
      '.custom-select',
      '.dropdown-content',
      '.option-item',
      '.search-box'
    ].some(selector => target.closest(selector));
    if (!isClickInside) {
      this.closeAllDropdowns();
    }
  }

  closeAllDropdowns(): void {
    this.isSocieteDropdownOpen = false;
    this.isChefDropdownOpen = false;
    this.isPaysDropdownOpen = false;
    this.isClientDropdownOpen = false;
  }
  closeModal(): void {
    this.overlayModalService.close();
  }
}
