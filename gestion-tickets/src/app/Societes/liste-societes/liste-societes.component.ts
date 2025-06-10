import { Component, OnInit } from '@angular/core';
import { SocieteService } from '../../_services/societe.service';
import { Societe } from '../../_models/societe';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaginatedResult } from '../../_models/pagination';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { SocieteFilterComponent } from '../../_filters/societe-filter/societe-filter.component';

@Component({
  selector: 'app-liste-societes',
  imports: [
    NgFor,
    FormsModule,
    RouterLink,
    NgIf,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    SocieteFilterComponent
  ],
  templateUrl: './liste-societes.component.html',
  styleUrls: ['./liste-societes.component.css']
})
export class ListeSocietesComponent implements OnInit {
  societes: Societe[] = [];
  // Variables pour la pagination
  pageNumber: number = 1;
  pageSize: number = 5;
  paginatedResult: PaginatedResult<Societe[]> | null = null;
  jumpPage!: number;
  // Terme de recherche (sera transmis au service)
  societesSearchTerm: string = '';

  filterParams: any = {};

  // Variables de chargement spécifiques pour chaque action
  isExportLoading: boolean = false;
  isDeleteLoading: boolean = false;
  isDeleteMultipleLoading: boolean = false;


  constructor(
    private societeService: SocieteService,
    public route: ActivatedRoute,
    private toastr: ToastrService,
    private overlayModalService: OverlayModalService
  ) {}

  ngOnInit(): void {
    this.jumpPage = this.pageNumber;
    this.loadSocietes();
  }

  // Lorsqu'on change le terme de recherche, on revient à la première page et on recharge
  onSearchChange(): void {
    this.pageNumber = 1;
    this.loadSocietes();
  }

  loadSocietes(): void {
    this.societeService
      .getPaginatedSocietes(this.pageNumber, this.pageSize, this.societesSearchTerm, this.filterParams)
      .subscribe({
        next: (result) => {
          this.paginatedResult = result;
          this.societes = result.items || [];
        },
        error: (error) => {
          console.error('Erreur lors du chargement des sociétés paginées', error);
          this.toastr.error("Erreur lors du chargement des sociétés paginées");
        }
      });
  }

  // Réception du filtre émis par le composant de filtre
  onFilter(filterValues: any): void {
    this.filterParams = filterValues;
    this.pageNumber = 1;
    this.loadSocietes();
  }

  // Pour la pagination, on travaille toujours avec l'API
  onPageChange(newPage: number): void {
    const maxPage = this.paginatedResult?.pagination?.totalPages || 1;
    this.pageNumber = Math.min(Math.max(newPage, 1), maxPage);
    this.jumpPage = this.pageNumber;
    this.loadSocietes();
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

  // Suppression d'une société via modal de confirmation (action individuelle)
  deleteSociete(id: number): void {
    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = "Êtes-vous sûr de vouloir supprimer cette société ?";

    modalInstance.confirmed.subscribe(() => {
      this.isDeleteLoading = true;
      this.societeService.deleteSociete(id).subscribe({
        next: () => {
          this.toastr.success("Société supprimée avec succès");
          this.loadSocietes();
          this.isDeleteLoading = false;
        },
        error: (error) => {
          console.error('Erreur lors de la suppression de la société', error);
          this.isDeleteLoading = false;
        }
      });
      this.overlayModalService.close();
    });

    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  // Sélectionner toutes les sociétés
  selectAll(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    this.societes = this.societes.map(societe => ({
      ...societe,
      selected: checkbox.checked
    }));
  }

  // Inverser la sélection d'une société
  toggleSelection(societe: Societe): void {
    societe.selected = !societe.selected;
  }

  // Suppression en masse des sociétés sélectionnées via modal de confirmation
  deleteSelectedSocietes(): void {
    const selectedSocietes = this.societes.filter(societe => societe.selected);
    if (selectedSocietes.length === 0) {
      this.toastr.warning("Aucune société sélectionnée.");
      return;
    }

    const modalInstance = this.overlayModalService.open(ConfirmModalComponent);
    modalInstance.message = "Êtes-vous sûr de vouloir supprimer les sociétés sélectionnées ?";

    modalInstance.confirmed.subscribe(() => {
      const selectedIds = selectedSocietes.map(s => s.id);
      this.isDeleteMultipleLoading = true;
      this.societeService.deleteSelectedSocietes(selectedIds).subscribe({
        next: () => {
          this.toastr.success("Les sociétés sélectionnées ont été supprimées avec succès.");
          this.loadSocietes();
          this.isDeleteMultipleLoading = false;
        },
        error: (error) => {
          console.error("Erreur lors de la suppression des sociétés", error);
          this.isDeleteMultipleLoading = false;
        }
      });
      this.overlayModalService.close();
    });

    modalInstance.cancelled.subscribe(() => {
      this.overlayModalService.close();
    });
  }

  range(start: number, end: number): number[] {
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  // Export des sociétés en Excel avec variable de chargement dédiée
  exportSocietes(): void {
    this.isExportLoading = true;
    this.societeService.exportSocietes(this.societesSearchTerm, this.filterParams)
      .subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `SocietesExport_${new Date().toISOString().replace(/[-:]/g, '').split('.')[0]}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
          this.isExportLoading = false;
        },
        error: (error) => {
          console.error("Erreur lors de l'export Excel des sociétés", error);
          this.toastr.error("Erreur lors de l'export Excel des sociétés");
          this.isExportLoading = false;
        }
      });
  }

  
}
