import { Component, ElementRef, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterOutlet } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule, Location } from '@angular/common';
import { NgSelectModule } from '@ng-select/ng-select';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { Projet } from '../../_models/Projet';
import { ProjetMember } from '../../_models/projet-member';
import { User } from '../../_models/user';
import { Pays } from '../../_models/pays';
import { Societe } from '../../_models/societe';

import { ProjetService } from '../../_services/projet.service';
import { AccountService } from '../../_services/account.service';
import { PaysService } from '../../_services/pays.service';
import { SocieteService } from '../../_services/societe.service';
import { UserSelectorDialogComponent } from '../../user-selector-dialog/user-selector-dialog.component';
import { PaginatedResult } from '../../_models/pagination';
import { ToastrService } from 'ngx-toastr';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { LoaderService } from '../../_services/loader.service';
import { GlobalLoaderService } from '../../_services/global-loader.service';
import { ClientDto } from 'src/app/DTOs/ClientDto';
import { ClientService } from 'src/app/_services/client.service';
import { ContratService } from 'src/app/_services/contrat.service';
import { Contrat } from 'src/app/_models/contrat';
import { TypeContrat } from 'src/app/DTOs/type-contrat.enum';

@Component({
  selector: 'app-details-projet',
  imports: [FormsModule, CommonModule, NgSelectModule, MatDialogModule, ReactiveFormsModule, RouterLink],
  templateUrl: './details-projet.component.html',
  styleUrls: ['./details-projet.component.scss']
})
export class DetailsProjetComponent implements OnInit {
  activeTab: 'personnels' | 'clients' | 'contrat' = 'clients';
  // --- Données du projet et membres ---
  projet!: Projet;
  membres: ProjetMember[] = [];
  projectClients: ClientDto[] = [];

  // Pagination (client-side) pour les membres du projet
  pageNumber: number = 1;
  pageSize: number = 9;
  jumpPage: number = 1;
  totalPages: number = 1;

  // Recherche dans le tableau des membres
  userSearchTerm: string = '';

  // --- Dropdown Pays ---
  pays: Pays[] = [];
  filteredPays: Pays[] = [];
  paysSearchTerm: string = '';
  isPaysDropdownOpen: boolean = false;

  // --- Dropdown Société ---
  societes: Societe[] = [];
  filteredSocietes: Societe[] = [];
  searchSociete: string = '';
  isSocieteDropdownOpen: boolean = false;

  // --- Mode édition ---
  editMode: boolean = false;

  // --- Gestion des utilisateurs (pour la boîte modale) ---
  availableUsers: Partial<User>[] = [];

  // (Optionnel) Pagination & recherche côté serveur pour les utilisateurs
  userPageNumber: number = 1;
  userPageSize: number = 1;
  userTotalPages: number = 1;
  totalUsers: number = 0;
  displayedUsers: User[] = [];
  userJumpPage: number = 1;

  isChefDropdownOpen: boolean = false;
  searchChef: string = '';
  availableChefs: User[] = [];
  filteredChefs: User[] = [];

  isLoading: boolean = false;

  contratForm!: FormGroup;

  // Variables de pagination et recherche pour clients
clientPageNumber: number = 1;
clientPageSize: number = 9;
clientJumpPage: number = 1;
clientTotalPages: number = 1;
clientSearchTerm: string = '';

  constructor(
    private route: ActivatedRoute,
    private projetService: ProjetService,
    public accountService: AccountService,
    private paysService: PaysService,
    private societeService: SocieteService,
    private toastr: ToastrService,
    public router: Router,
    private dialog: MatDialog,
    private contratService: ContratService,
    private fb: FormBuilder,
    private clientService: ClientService,
    private overlayModalService: OverlayModalService,
    private loaderService: LoaderService,
    private globalLoaderService: GlobalLoaderService
  ) {
    this.loaderService.isLoading$.subscribe((loading) => {
      this.isLoading = loading;
    });
  }

  ngOnInit(): void {
    // S'abonner aux paramètres de la route pour détecter les changements de l'ID du projet
    this.route.params.subscribe(params => {
      const id = Number(params['id']);
      if (id) {
        this.getProjetDetails(id);
      }
    });
    this.getAvailableUsers();
    this.getAvailableChefs();
    this.loadPays();
    this.loadSocietes();
    this.loadUsers(); // Pour la recherche/pagination côté serveur (si nécessaire)
    this.initContratForm();
  }

  // --- Chargement du projet et de ses membres ---
  getProjetDetails(id: number): void {
    this.globalLoaderService.showGlobalLoader();
    this.projetService.getProjetById(id).subscribe({
      next: (data) => {
        this.projet = data;
        this.getMembres();
        if (this.activeTab === 'clients') {
          this.loadProjectClients();
        }
        if (this.projet.contrat) {
          const rawDeb = this.projet.contrat.dateDebut as string;
          const rawFin = this.projet.contrat.dateFin  as string;
          const isoDeb = rawDeb.split('T')[0];
          const isoFin = rawFin.split('T')[0];
          this.contratForm.patchValue({
            id:        this.projet.contrat.id,
            dateDebut: isoDeb,
            dateFin:   isoFin,
            type:      this.projet.contrat.type 
          });
        }
        

      },
      error: (err) => {
        console.error('Erreur lors de la récupération du projet', err);
        this.toastr.error('Erreur lors de la récupération du projet');
      },
      complete: () => {
        this.globalLoaderService.hideGlobalLoader();
      }
    });
  }

  getAvailableChefs(): void {
    this.accountService.getAllUsers().subscribe({
      next: (users: User[]) => {
        // Vous pouvez appliquer ici un filtre sur les rôles si nécessaire.
        this.availableChefs = users.filter(user => {
          const role = user.role.toLowerCase().trim();
          // Par exemple, pour inclure les utilisateurs avec le rôle "chef de projet" ou "collaborateur"
          return role === 'chef de projet' || role === 'collaborateur';
        });
        this.filteredChefs = [...this.availableChefs];
      },
      error: (err) => console.error("Erreur lors de la récupération des utilisateurs", err)
    });
  }


  getMembres(): void {
    if (this.projet && this.projet.id) {
      this.projetService.getMembresProjet(this.projet.id).subscribe({
        next: (data: ProjetMember[]) => {
          // Affecte la liste des membres récupérée
          this.membres = data;
          // Si un chef de projet est défini et qu'il n'est pas déjà dans la liste, on l'ajoute
          if (this.projet.chefProjet && !this.membres.some(m => m.userId === this.projet.chefProjet!.id)) {
            const chefMember: ProjetMember = {
              projetId: this.projet.id, // Ajout de la propriété manquante
              userId: this.projet.chefProjet.id,
              firstName: this.projet.chefProjet.firstName,
              lastName: this.projet.chefProjet.lastName,
              role: this.projet.chefProjet.role ? this.projet.chefProjet.role : 'Chef de projet',
              selected: false
            };
            this.membres.unshift(chefMember);
          }
        },
        error: (err) => { console.error('Erreur lors du chargement des membres', err); }
      });
    }
  }


  // --- Filtrage et pagination des membres (client-side) ---
  get displayedMembres(): ProjetMember[] {
    let filteredMembres = this.membres;
    if (this.userSearchTerm && this.userSearchTerm.trim() !== '') {
      const term = this.userSearchTerm.toLowerCase();
      filteredMembres = this.membres.filter(m =>
        (m.firstName + ' ' + m.lastName).toLowerCase().includes(term)
      );
    }
    this.totalPages = Math.ceil(filteredMembres.length / this.pageSize) || 1;
    const start = (this.pageNumber - 1) * this.pageSize;
    return filteredMembres.slice(start, start + this.pageSize);
  }

  onUserSearch(): void {
    this.pageNumber = 1;
  }

  onPageChange(newPage: number): void {
    this.pageNumber = Math.min(Math.max(newPage, 1), this.totalPages);
    this.jumpPage = this.pageNumber;
  }

  jumpToPage(): void {
    if (this.jumpPage >= 1 && this.jumpPage <= this.totalPages) {
      this.pageNumber = this.jumpPage;
    } else {
      console.warn('Numéro de page invalide');
    }
  }

  selectAll(event: any): void {
    const checked = event.target.checked;
    this.membres.forEach(m => m.selected = checked);
  }

  toggleSelection(membre: ProjetMember): void {
    console.log('Membre sélectionné/désélectionné :', membre);
  }

  // --- Recherche et pagination côté serveur pour les utilisateurs (pour la modale, si nécessaire) ---
  loadUsers(): void {
    this.accountService.getUsers(this.userPageNumber, this.userPageSize, this.userSearchTerm)
      .subscribe((result: PaginatedResult<User[]>) => {
        this.displayedUsers = result.items ?? [];
        this.userTotalPages = result.pagination?.totalPages ?? 1;
      }, error => {
        console.error('Erreur lors du chargement des utilisateurs paginés', error);
      });
  }

  // --- Navigation (exemple de redirection) ---
  viewProjet(projetId: number): void {
    this.router.navigate(['/home/projets/details', projetId]);
  }

  // --- Gestion des dropdowns pour Pays ---
  loadPays(): void {
    this.paysService.getPays(this.paysSearchTerm).subscribe({
      next: (data) => {
        this.filteredPays = data;
        this.pays = data;
      },
      error: (err) => { console.error('Erreur lors de la récupération des pays', err); }
    });
  }

  onPaysSearch(): void {
    this.loadPays();
  }

  // --- Gestion des dropdowns pour Société ---
  loadSocietes(): void {
    this.societeService.getSocietes(this.searchSociete).subscribe({
      next: (data) => {
        this.filteredSocietes = data;
        this.societes = data;
      },
      error: (err) => { console.error('Erreur lors de la récupération des sociétés', err); }
    });
  }

  onSocieteSearch(): void {
    this.loadSocietes();
  }

  // Empêcher la propagation du clic dans les dropdowns
  toggleDropdown(type: string): void {
    if (type === 'pays') {
      this.isPaysDropdownOpen = !this.isPaysDropdownOpen;
      if (this.isPaysDropdownOpen) {
        this.isSocieteDropdownOpen = false;
        this.isChefDropdownOpen = false;
      }
    } else if (type === 'societe') {
      this.isSocieteDropdownOpen = !this.isSocieteDropdownOpen;
      if (this.isSocieteDropdownOpen) {
        this.isPaysDropdownOpen = false;
        this.isChefDropdownOpen = false;
      }
    } else if (type === 'chef') {
      this.isChefDropdownOpen = !this.isChefDropdownOpen;
      if (this.isChefDropdownOpen) {
        this.isPaysDropdownOpen = false;
        this.isSocieteDropdownOpen = false;
      }
    }
  }



  // Récupération du nom du pays à partir de son identifiant
  getPaysName(idPays: number): string {
    return this.pays.find(p => p.idPays === idPays)?.nom || '';
  }

  // Récupération du nom de la société à partir de son identifiant
  getSocieteName(idSociete: number | null | undefined): string {
    if (!idSociete) {
      return '';
    }
    return this.societes.find(s => s.id === idSociete || s.id === idSociete)?.nom || '';
  }


  // Méthode de sélection d'une société dans le dropdown
  selectSociete(societe: any): void {
    // Affecte l'identifiant de la société
    this.projet.societeId = societe.id || societe.idSociete;
    // Affecte automatiquement l'id du pays de la société au projet
    this.projet.idPays = societe.paysId;
    // Ferme le dropdown
    this.isSocieteDropdownOpen = false;
    // Réinitialise le champ de recherche et recharge la liste complète
    this.searchSociete = '';
    this.loadSocietes();
  }


  // --- Sauvegarde, annulation et suppression du projet ---
  saveProjet(): void {
    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = "Confirmez-vous la modification du projet ?";
    modalInstance.confirmed.subscribe(() => {
      this.loaderService.showLoader();
      this.projetService.updateProjet(this.projet).subscribe({
        next: () => {
          this.toastr.success('Projet mis à jour avec succès');
          this.editMode = false;
          this.getProjetDetails(this.projet.id);
          this.loaderService.hideLoader();
        },
        error: (err) => {
          console.error('Erreur lors de la mise à jour du projet', err);
          this.toastr.error('Erreur lors de la mise à jour du projet');
          this.loaderService.hideLoader();
        }
      });
      this.overlayModalService.close();
    });
    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }


  cancelEdit(): void {
    this.editMode = false;
    // Recharger les détails du projet en passant l'ID du projet
    this.getProjetDetails(this.projet.id);
  }



  // --- Gestion des utilisateurs dans la modale ---
  openUserSelector(): void {
    const dialogRef = this.dialog.open(UserSelectorDialogComponent, {
      data: { availableUsers: this.availableUsers }
    });

    dialogRef.afterClosed().subscribe((selectedUser: User) => {
      if (selectedUser) {
        this.addUserToProjet(selectedUser);
      }
    });
  }

  getAvailableUsers(): void {
    this.accountService.getAllUsers().subscribe({
      next: (users: User[]) => {
        this.availableUsers = users.map(user => ({
          id: user.id,
          firstName: user.firstName,
          lastName: user.lastName,
          role: user.role,
          email: user.email
        }));
      },
      error: (err) => console.error("Erreur lors de la récupération des utilisateurs", err)
    });
  }


  addUserToProjet(user: User): void {
    if (this.projet && this.projet.id && user.id) {
      this.projetService.ajouterUtilisateurAuProjet(this.projet.id, user.id, user.role)
        .subscribe({
          next: () => {
            // Par exemple, afficher une notification de succès
            this.toastr.success('Utilisateur ajouté avec succès');
            this.getMembres();
          },
          error: (err) => {
            if (err.status === 409) {
              //this.toastr.error(err.message, 'Erreur');
            } else {
              console.error('Erreur lors de l’ajout de l’utilisateur', err);
            }
          }
        });
    } else {
      this.toastr.warning("Veuillez sélectionner un utilisateur valide.");
    }
  }


  removeUser(userId: number): void {
    if (this.projet && this.projet.id) {
      const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
      modalInstance.message = 'Confirmer la suppression de cet utilisateur du projet ?';
      modalInstance.confirmed.subscribe(() => {
        this.loaderService.showLoader();
        this.projetService.supprimerUtilisateurDuProjet(this.projet.id, userId)
          .subscribe({
            next: () => {
              this.toastr.success('Utilisateur retiré avec succès');
              this.getMembres();
              this.loaderService.hideLoader();
            },
            error: (err) => {
              console.error('Erreur lors du retrait de l’utilisateur', err);
              this.loaderService.hideLoader();
            }
          });
        this.overlayModalService.close();
      });
      modalInstance.cancelled.subscribe(() => {
        this.overlayModalService.close();
      });
    }
  }


  // --- Utilitaire pour générer une plage de nombres pour la pagination ---
  range(start: number, end: number): number[] {
    return Array(end - start + 1).fill(0).map((_, i) => start + i);
  }


  deleteSelectedMembers(): void {
    const selectedUserIds = this.membres.filter(m => m.selected).map(m => m.userId);
    if (selectedUserIds.length === 0) {
      this.toastr.warning("Aucun membre sélectionné pour la suppression.");
      return;
    }
    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = "Êtes-vous sûr de vouloir retirer les membres sélectionnés du projet ?";
    modalInstance.confirmed.subscribe(() => {
      this.loaderService.showLoader();
      selectedUserIds.forEach(userId => {
        this.projetService.supprimerUtilisateurDuProjet(this.projet.id, userId)
          .subscribe({
            next: () => this.getMembres(),
            error: err => console.error("Erreur lors du retrait de l’utilisateur", err)
          });
      });
      this.loaderService.hideLoader();
      this.overlayModalService.close();
    });
    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  onChefSearch(): void {
    if (this.searchChef && this.searchChef.trim() !== '') {
      const term = this.searchChef.toLowerCase();
      this.filteredChefs = this.availableChefs.filter(chef =>
        (chef.firstName + ' ' + chef.lastName).toLowerCase().includes(term)
      );
    } else {
      this.filteredChefs = [...this.availableChefs];
    }
  }

  selectChef(chef: User): void {
    // Met à jour l'identifiant du chef dans le projet
    this.projet.chefProjetId = chef.id;
    // Optionnel : Mettre à jour l'objet complet du chef
    this.projet.chefProjet = chef;
    // Fermer le dropdown
    this.isChefDropdownOpen = false;
    // Réinitialiser le champ de recherche si besoin
    this.searchChef = '';
    this.filteredChefs = [...this.availableChefs];
  }

  getChefName(chefId: number | undefined): string {
    if (chefId === undefined) return '';
    const chef = this.availableChefs.find(c => c.id === chefId);
    return chef ? `${chef.firstName} ${chef.lastName}` : '';
  }



  // Gestionnaire de clic global pour fermer les dropdowns
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    // Vérifiez si le clic se produit en dehors d'un élément ayant la classe 'custom-select'
    if (!target.closest('.custom-select')) {
      this.isPaysDropdownOpen = false;
      this.isSocieteDropdownOpen = false;
      this.isChefDropdownOpen = false;
    }
  }

  /** Type-guard : est-ce un User « interne » (avec .role) ? */
  private isUser(u: User | ClientDto | null): u is User {
    return !!u && (u as User).role !== undefined;
  }

  /** Type-guard : est-ce un vrai ClientDto (depuis la table clients) ? */
  private isClientDto(u: User | ClientDto | null): u is ClientDto {
    return !!u && (u as ClientDto).paysId !== undefined;
  }

  /** Raccourci vers l’utilisateur courant (User ou ClientDto ou null) */
  private get currentUser(): User | ClientDto | null {
    return this.accountService.currentUser();
  }

  /** Le user est un super admin ? */
  isSuperAdmin(): boolean {
    const cu = this.currentUser;
    if (!this.isUser(cu)) return false;
    return cu.role.toLowerCase().trim() === 'super admin';
  }

  /** Le user est un chef de projet ? */
  isChefDeProjet(): boolean {
    const cu = this.currentUser;
    if (!this.isUser(cu)) return false;
    return cu.role.toLowerCase().trim() === 'chef de projet';
  }

  /** Le user est un collaborateur ? */
  isCollaborateur(): boolean {
    const cu = this.currentUser;
    if (!this.isUser(cu)) return false;
    return cu.role.toLowerCase().trim() === 'collaborateur';
  }

  /** L’objet courant est un ClientDto (venant de la table clients) ? */
  isClient(): boolean {
    return this.isClientDto(this.currentUser);
  }

  switchTab(tab: 'personnels' | 'clients' | 'contrat') {
    this.activeTab = tab;
    if (tab === 'clients') {
      this.loadProjectClients();
    }
  }
  private loadProjectClients() {
    this.projetService.getClientProjects(this.projet.id)
      .subscribe(clients => this.projectClients = clients);
  }


  onSubmitContrat(): void {
    const id = this.contratForm.value.id;
    if (id == null) {
      this.toastr.error("Impossible de déterminer l'ID du contrat.");
      return;
    }
    // Vérifier qu'un contrat de projet est chargé
    if (!this.projet.contrat) {
      this.toastr.error("Aucun contrat de projet n'est chargé.");
      return;
    }

    // Si rien n'a changé
    if (!this.contratForm.dirty) {
      this.toastr.warning("Veuillez modifier au moins un champ.");
      return;
    }

    // Formulaire invalide
    if (this.contratForm.invalid) {
      this.toastr.error("Veuillez corriger les erreurs du formulaire.");
      return;
    }

    // Construire l'objet à soumettre
    const updated: Contrat = {
      id: this.contratForm.value.id,
      dateDebut: this.contratForm.value.dateDebut,
      dateFin: this.contratForm.value.dateFin,
      type: this.contratForm.value.type as TypeContrat,
      societePartenaireId: this.projet.societeId ?? undefined,
      clientId: this.projet.contrat.clientId ?? undefined
    };

    this.loaderService.showLoader();
    this.contratService.updateContract(updated.id, updated)
      .subscribe({
        next: () => {
          this.toastr.success("Contrat de projet mis à jour avec succès.");
          // mettre à jour localement
          this.projet.contrat = updated;
          this.loaderService.hideLoader();
        },
        error: (err) => {
          console.error(err);
          this.toastr.error("Erreur lors de la mise à jour du contrat.");
          this.loaderService.hideLoader();
        }
      });
  }

  cancelContrat(): void {
    if (this.projet && this.projet.contrat) {
      this.contratForm.patchValue({
        dateDebut: this.projet.contrat.dateDebut,
        dateFin: this.projet.contrat.dateFin,
      });
    }
  }
  initializeContratForm(): void {
    this.initContratForm();
  }
  initContratForm(): void {
    this.contratForm = this.fb.group({
      id: [0],
      dateDebut: ['', Validators.required],
      dateFin: [''],
      type: [TypeContrat.Projet, Validators.required]
    });
  }

  confirmDetachClient(clientId: number) {
    const modalRef = this.overlayModalService.open(ConfirmModalComponent);
    modalRef.message = 'Voulez‑vous vraiment détacher ce client du projet ?';
    modalRef.confirmed.subscribe(() => {
      this.detachClient(clientId);
      this.overlayModalService.close();
    });
    modalRef.cancelled.subscribe(() => this.overlayModalService.close());
  }

  /** Détache le client via l’API et met à jour la liste locale */
  private detachClient(clientId: number) {
    if (!this.projet?.id) return;
    this.loaderService.showLoader();
    this.clientService
      .detachProjectFromClient(clientId, this.projet.id)
      .subscribe({
        next: () => {
          this.toastr.success('Client détaché avec succès');
          // Retirer le client de la liste affichée
          this.projectClients = this.projectClients.filter(c => c.id !== clientId);
        },
        error: err => {
          console.error('Erreur lors du détachement', err);
          this.toastr.error('Impossible de détacher le client');
        },
        complete: () => this.loaderService.hideLoader()
      });
  }

  // Sélection de clients
selectAllClients(event: any): void {
  const checked = event.target.checked;
  this.projectClients.forEach(c => (c as any).selected = checked);
}

toggleClientSelection(client: any): void {
  // Optionnel : actions lors de la sélection/désélection
}

get displayedClients(): any[] {
  let filtered = this.projectClients;
  if (this.clientSearchTerm.trim()) {
    const term = this.clientSearchTerm.toLowerCase();
    filtered = filtered.filter(c => (c.firstName + ' ' + c.lastName).toLowerCase().includes(term));
  }
  this.clientTotalPages = Math.ceil(filtered.length / this.clientPageSize) || 1;
  const start = (this.clientPageNumber - 1) * this.clientPageSize;
  return filtered.slice(start, start + this.clientPageSize);
}

onClientSearch(): void { this.clientPageNumber = 1; }

onClientPageChange(page: number): void {
  this.clientPageNumber = Math.min(Math.max(page, 1), this.clientTotalPages);
  this.clientJumpPage = this.clientPageNumber;
}

jumpToClientPage(): void {
  if (this.clientJumpPage >= 1 && this.clientJumpPage <= this.clientTotalPages) {
    this.clientPageNumber = this.clientJumpPage;
  }
}

deleteSelectedClients(): void {
  const ids = this.projectClients.filter((c: any) => c.selected).map(c => c.id);
  if (!ids.length) {
    this.toastr.warning('Aucun client sélectionné pour la suppression.');
    return;
  }
  const modal = this.overlayModalService.open(ConfirmModalComponent);
  modal.message = 'Confirmer la suppression des clients sélectionnés ?';
  modal.confirmed.subscribe(() => {
    ids.forEach(id => this.clientService.detachProjectFromClient(id, this.projet.id).subscribe(() => {
      this.projectClients = this.projectClients.filter(c => !ids.includes(c.id));
    }));
    this.overlayModalService.close();
  });
  modal.cancelled.subscribe(() => this.overlayModalService.close());
}

// Méthode pour ouvrir le sélecteur de clients
openClientSelector(): void {
  this.clientService.getAll().subscribe({
    next: (clients) => {
      const dialogRef = this.dialog.open(UserSelectorDialogComponent, {
        data: { availableUsers: clients }
      });
      dialogRef.afterClosed().subscribe((selectedClient: ClientDto) => {
        if (selectedClient && this.projet && this.projet.id) {
          this.clientService.addClientToProject(selectedClient.id, this.projet.id)
          .subscribe(
            () => {
              this.toastr.success('Client ajouté avec succès');
              this.loadProjectClients();
            },
            (err) => {
              console.error('Erreur lors de l’ajout du client', err);
              if (err.status === 409) {
                //this.toastr.error(err.error, 'Erreur 409');
              } else {
                const message = err.error || 'Une erreur est survenue lors de l\'ajout du client.';
                this.toastr.error(message, 'Erreur');
              }
            }
          );          
        }
      });
    },
    error: (err) => {
      console.error('Erreur lors de la récupération des clients', err);
    }
  });
}
}
