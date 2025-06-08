import { NgFor, NgIf, NgClass, CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Router, RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { PaginatedResult } from 'src/app/_models/pagination';
import { ClientService } from 'src/app/_services/client.service';
import { OverlayModalService } from 'src/app/_services/overlay-modal.service';
import { ConfirmModalComponent } from 'src/app/confirm-modal/confirm-modal.component';
import { ClientDto } from 'src/app/DTOs/ClientDto';
import { ClientParams } from 'src/app/DTOs/ClientParams';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelect, MatSelectModule    } from '@angular/material/select';
import { ClientFilterComponent } from 'src/app/_filters/client-filter/client-filter.component';

@Component({
  selector: 'app-list-clients',
  imports: [ NgFor, NgIf, NgClass, FormsModule, RouterLink, CommonModule,
    MatMenuModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    ClientFilterComponent],
  templateUrl: './list-clients.component.html',
  styleUrl: './list-clients.component.css'
})
export class ListClientsComponent implements OnInit {
  @ViewChild('projectsSelect') matSelect!: MatSelect;
  pageNumber = 1;
  pageSize = 9;
  // On définit PaginatedResult générique sur ClientDto[] pour que items soit un tableau
  paginatedResult: PaginatedResult<ClientDto[]> | null = null;
  jumpPage = 1;
  clientsSearchTerm = '';
  filterParams: Partial<ClientParams> = {};
  newClientId: number | null = null;
  isExportLoading = false;
  isDeleteLoading = false;
  selectedClientIds = new Set<number>();

  constructor(
    private clientService: ClientService,
    private overlayModalService: OverlayModalService,
    private toastr: ToastrService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.getClients();
  }

  getClients(): void {
    const params: ClientParams = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.clientsSearchTerm,
      ...this.filterParams
    } as ClientParams;

    this.clientService.getPaged(params).subscribe({
      next: res => {
        // res.items est du type ClientDto[] | undefined
        const items: ClientDto[] = res.items ?? [];
        // Initialiser la sélection
        items.forEach(c => (c as any).selected = false);
        this.paginatedResult = { ...res, items };
      },
      error: err => console.error('Erreur chargement clients', err)
    });
  }

  onFilter(filter: Partial<ClientParams>): void {
    this.filterParams = filter;
    this.pageNumber = 1;
    this.getClients();
  }

  onSearchChange(): void {
    this.pageNumber = 1;
    this.getClients();
  }

  onPageChange(newPage: number): void {
    const max = this.paginatedResult?.pagination?.totalPages ?? 1;
    this.pageNumber = Math.max(1, Math.min(newPage, max));
    this.jumpPage = this.pageNumber;
    this.getClients();
  }

  jumpToPage(): void {
    const max = this.paginatedResult?.pagination?.totalPages ?? 1;
    this.pageNumber = Math.max(1, Math.min(this.jumpPage, max));
    this.getClients();
  }

  selectAll(evt: Event): void {
    const checked = (evt.target as HTMLInputElement).checked;
    this.paginatedResult?.items?.forEach(c => (c as any).selected = checked);
  }

  isSelected(client: ClientDto): boolean {
    return this.selectedClientIds.has(client.id);
  }
  
  toggleSelection(client: ClientDto): void {
    if (this.selectedClientIds.has(client.id)) {
      this.selectedClientIds.delete(client.id);
    } else {
      this.selectedClientIds.add(client.id);
    }
  }

  deleteClient(client: ClientDto): void {
    const modal = this.overlayModalService.open(ConfirmModalComponent);
    modal.message = `Supprimer le client ${client.firstName}  ${client.lastName}?`;
    modal.confirmed.subscribe(() => {
      this.isDeleteLoading = true;
      this.clientService.delete(client.id).subscribe({
        next: () => {
          this.toastr.success('Client supprimé.');
          this.getClients();
        },
        error: () => {},
        complete: () => this.isDeleteLoading = false
      });
      this.overlayModalService.close();
    });
    modal.cancelled.subscribe(() => this.overlayModalService.close());
  }

  exportClients(): void {
    const params: ClientParams = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.clientsSearchTerm,
      ...this.filterParams
    } as ClientParams;

    this.isExportLoading = true;
    this.clientService.export(params).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `ClientsExport_${new Date().toISOString().slice(0,19).replace(/[:T]/g,'')}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.isExportLoading = false;
      },
      error: () => this.isExportLoading = false
    });
  }

  range(start: number, end: number): number[] {
    return Array(end - start + 1).fill(0).map((_, i) => start + i);
  }
  getProjectIds(client: ClientDto): number[] {
    return client.projets.map(p => p.id);
  }

  onProjectSelect(projetId: number) {
    // vide la sélection pour pouvoir recliquer sur le même projet
    setTimeout(() => this.matSelect.writeValue(null), 0);
    this.viewProjet(projetId);
  }
  
  viewProjet(projetId: number) {
    this.router.navigate(['/home/Projets/details', projetId]);
  }
}
