import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
import { Pays } from '../../_models/pays';
import { Societe } from '../../_models/societe';
import { ProjetService } from '../../_services/projet.service';
import { SocieteService } from '../../_services/societe.service';
import { PaysService } from '../../_services/pays.service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Projet } from '../../_models/Projet';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, NgIf } from '@angular/common';
import { AccountService } from '../../_services/account.service';
import { User } from '../../_models/user';
import { forkJoin } from 'rxjs';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { DropdownService } from './../../_services/dropdown.service';
import { ToastrService } from 'ngx-toastr';
import { MatSidenavModule } from '@angular/material/sidenav';
import { LoaderService } from '../../_services/loader.service';
import { ProjetCreate } from 'src/app/DTOs/projet-create.model';

@Component({
  selector: 'app-ajouter-projet',
  imports: [CommonModule, FormsModule, NgIf, RouterLink, ReactiveFormsModule, MatSidenavModule],
  templateUrl: './ajouter-projet.component.html',
  styleUrls: ['./ajouter-projet.component.css']
})
export class AjouterProjetComponent implements OnInit {
  @Input() societeId!: number;
  @Output() projetCreated = new EventEmitter<Projet>();
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
  isLoading: boolean = false;

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
    private toastr: ToastrService,
    private loaderService: LoaderService
  ) {
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
   }

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
      societeId: [null, this.isSocieteProjet ? Validators.required : []],
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
      this.projetForm.updateValueAndValidity();
      this.toastr.error("Veuillez remplir tous les champs obligatoires.");
      return;
    }
  
    const fv = this.projetForm.value;
    const dto: ProjetCreate = {
      nom:          fv.nom,
      description:  fv.description,
      chefProjetId: fv.chefProjetId,   // toujours un number ici
      societeId:    fv.societeId       // toujours un number ici
    };
  
    // Active le loader avant l'appel
    this.loaderService.showLoader();
    this.projetService.addProjet(dto).subscribe({
      next: (projetCree) => {
        this.toastr.success('Projet créé avec succès');
        this.router.navigate(['/home/Projets']);
        this.loaderService.hideLoader();
      },
      error: (error) => {
        console.error('Erreur ajout projet', error);
        let errMsg = "Erreur lors de l'ajout du projet.";
        // Gestion de l'erreur (formatage du message)
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
        this.loaderService.hideLoader();
      }
    });
  }

  ajouterUtilisateursAuProjet(projetId: number): void {
    const requests = [];

    // Ajout du chef de projet
    const chefId = this.projetForm.get('chefProjetId')?.value;
    if (chefId) {
      requests.push(
        this.projetService.ajouterUtilisateurAuProjet(projetId, chefId, 'Chef de Projet')
      );
    }

    // Si d'autres utilisateurs (développeurs, par exemple) sont à ajouter, on peut les parcourir ici

    if (requests.length === 0) {
      this.router.navigate(['/home/Projets']);
      return;
    }

    forkJoin(requests).subscribe({
      next: () => {
        this.toastr.success('Utilisateurs ajoutés au projet avec succès');
        this.router.navigate(['/home/Projets']);
      },
      error: (err) => {
        console.error('Erreur lors de l\'ajout des utilisateurs', err.error || err);
        this.toastr.error('Erreur lors de l\'ajout des utilisateurs');
      }
    });
  }

  toggleDropdown(type?: string): void {
    if (type) {
      switch (type) {
        case 'pays':
          this.isPaysDropdownOpen = !this.isPaysDropdownOpen;
          if (this.isPaysDropdownOpen) {
            this.filteredPays = [...this.pays];
          }
          break;
        case 'societe':
          this.isSocieteDropdownOpen = !this.isSocieteDropdownOpen;
          if (this.isSocieteDropdownOpen) {
            this.filteredSocietes = [...this.societes];
          }
          break;
        case 'chef':
          this.isChefDropdownOpen = !this.isChefDropdownOpen;
          if (this.isChefDropdownOpen) {
            this.filteredChefs = [...this.chefsProjet];
          }
          break;
        case 'client':
          this.isClientDropdownOpen = !this.isClientDropdownOpen;
          if (this.isClientDropdownOpen) {
            this.filteredClients = [...this.clients];
          }
          break;
      }
    }
  }

  filterItems(type: string): void {
    switch (type) {
      case 'societe':
        this.filteredSocietes = this.societes.filter(s =>
          s.nom.toLowerCase().includes(this.searchSociete.toLowerCase())
        );
        break;
      case 'chef':
        this.filteredChefs = this.chefsProjet.filter(c =>
          `${c.firstName} ${c.lastName}`.toLowerCase().includes(this.searchChef.toLowerCase())
        );
        break;
      case 'client':
        this.filteredClients = this.clients.filter(c =>
          `${c.firstName} ${c.lastName}`.toLowerCase().includes(this.searchClient.toLowerCase())
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
}
