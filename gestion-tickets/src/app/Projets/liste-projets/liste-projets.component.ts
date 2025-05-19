import { ToastrService } from 'ngx-toastr';
import { Component, OnInit } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaginatedResult } from '../../_models/pagination';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Projet } from '../../_models/Projet';
import { ProjetService } from '../../_services/projet.service';
import { AccountService } from '../../_services/account.service';

// Importations pour le modal de confirmation
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ProjetFilterComponent } from '../../_filters/projet-filter/projet-filter.component';

@Component({
  selector: 'app-liste-projets',
  imports: [
    NgFor,
    NgIf,
    FormsModule,
    RouterLink,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    ProjetFilterComponent
  ],
  templateUrl: './liste-projets.component.html',
  styleUrls: ['./liste-projets.component.css']
})
export class ListeProjetsComponent implements OnInit {
  paginatedResult: PaginatedResult<Projet[]> | null = null;
  pageNumber: number = 1;
  pageSize: number = 5;
  jumpPage!: number;
  projetsSearchTerm: string = '';

  currentFilters: any = {};

  // Variables de chargement spécifiques pour chaque action
  isExportLoading: boolean = false;
  isDeleteLoading: boolean = false;
  isDeleteMultipleLoading: boolean = false;

  constructor(
    private projetsService: ProjetService,
    public accountService: AccountService,
    public route: ActivatedRoute,
    private overlayModalService: OverlayModalService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.jumpPage = this.pageNumber;
    this.route.data.subscribe(() => {
      this.getProjets();
    });
  }

  onSearchChange(): void {
    this.pageNumber = 1;
    this.getProjets();
  }

  getProjets(): void {
    const cu = this.accountService.currentUser();
    // Appliquer les guards pour éviter l'accès direct à .role ou .id sur ClientDto
    const filters = {
      ...this.currentFilters,
      searchTerm: this.projetsSearchTerm,
      role: this.isUser(cu) ? cu.role : undefined,
      userId: this.isUser(cu) ? cu.id : undefined
    };

    this.projetsService.getPaginatedProjets(this.pageNumber, this.pageSize, filters.searchTerm, filters)
      .subscribe({
        next: (response) => {
          this.paginatedResult = response;
        },
        error: (error) => {
          console.error('Erreur lors du chargement des projets paginés', error);
          this.toastr.error('Erreur lors du chargement des projets paginés');
        }
      });
  }

  // Getter qui filtre la liste actuelle en fonction de la recherche
  get filteredProjets(): Projet[] {
    const projets = this.paginatedResult?.items || [];
    if (!this.projetsSearchTerm.trim()) {
      return projets;
    }
    const lowerTerm = this.projetsSearchTerm.toLowerCase();
    return projets.filter(projet =>
      projet.nom.toLowerCase().includes(lowerTerm)
    );
  }

  // Gestion de la pagination
  onPageChange(newPage: number): void {
    const maxPage = this.paginatedResult?.pagination?.totalPages || 1;
    this.pageNumber = Math.min(Math.max(newPage, 1), maxPage);
    this.jumpPage = this.pageNumber;
    this.getProjets();
  }

  jumpToPage(): void {
    const totalPages = this.paginatedResult?.pagination?.totalPages || 1;
    const page = Number(this.jumpPage);
    if (!isNaN(page) && page >= 1 && page <= totalPages) {
      this.onPageChange(page);
    } else {
      console.warn('Numéro de page invalide');
    }
  }

  // Suppression d'un seul projet avec confirmation via modal
  deleteProjet(id: number): void {
    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = "Êtes-vous sûr de vouloir supprimer ce projet ?";

    modalInstance.confirmed.subscribe(() => {
      this.isDeleteLoading = true;
      this.projetsService.deleteProjet(id).subscribe({
        next: () => {
          this.toastr.success("Projet supprimé avec succès");
          this.getProjets();
          this.isDeleteLoading = false;
        },
        error: (err) => {
          console.error("Erreur lors de la suppression du projet", err);
          this.isDeleteLoading = false;
        }
      });
      this.overlayModalService.close();
    });

    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  selectAll(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (this.paginatedResult?.items) {
      this.paginatedResult.items.forEach(projet => projet.selected = checkbox.checked);
    }
  }

  toggleSelection(projet: Projet, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    projet.selected = checkbox.checked;
  }

  range(start: number, end: number): number[] {
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  // Suppression en masse des projets sélectionnés avec confirmation via modal
  deleteSelectedProjects(): void {
    const projets = this.paginatedResult?.items || [];
    const selectedIds = projets.filter(projet => projet.selected).map(projet => projet.id);

    if (selectedIds.length === 0) {
      this.toastr.warning("Aucun projet sélectionné pour suppression.");
      return;
    }

    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = "Êtes-vous sûr de vouloir supprimer les projets sélectionnés ?";

    modalInstance.confirmed.subscribe(() => {
      this.isDeleteMultipleLoading = true;
      this.projetsService.deleteSelectedProjets(selectedIds).subscribe({
        next: () => {
          this.toastr.success("Les projets sélectionnés ont été supprimés avec succès.");
          this.getProjets();
          this.isDeleteMultipleLoading = false;
        },
        error: error => {
          this.isDeleteMultipleLoading = false;
        }
      });
      this.overlayModalService.close();
    });

    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  onApplyFilter(filters: any): void {
    this.currentFilters = filters;
    this.pageNumber = 1;
    this.getProjets();
  }

  // Export des projets en Excel
  exportProjets(): void {
    this.isExportLoading = true;
    this.projetsService.exportProjets(this.currentFilters).subscribe({
      next: (fileBlob: Blob) => {
        const objectUrl = URL.createObjectURL(fileBlob);
        const a = document.createElement('a');
        a.href = objectUrl;
        a.download = `ProjetsExport_${new Date().getTime()}.xlsx`;
        a.click();
        URL.revokeObjectURL(objectUrl);
        this.isExportLoading = false;
      },
      error: (err) => {
        console.error("Erreur lors de l'export des projets", err);
        this.toastr.error("Erreur lors de l'export des projets");
        this.isExportLoading = false;
      }
    });
  }

  /** Type-guard: vérifie que currentUser est un User */
  private isUser(u: any): u is { role: string } {
    return !!u && typeof u.role === 'string';
  }

  /** Raccourci vers currentUser */
  private get currentUser() {
    return this.accountService.currentUser();
  }

  isSuperAdmin(): boolean {
    const cu = this.currentUser;
    return this.isUser(cu) && cu.role.toLowerCase().trim() === 'super admin';
  }

  isChefDeProjet(): boolean {
    const cu = this.currentUser;
    return this.isUser(cu) && cu.role.toLowerCase().trim() === 'chef de projet';
  }

  /** Uniquement les deux précédents rôles ont accès */
  canManage(): boolean {
    return this.isSuperAdmin() || this.isChefDeProjet();
  }
}
