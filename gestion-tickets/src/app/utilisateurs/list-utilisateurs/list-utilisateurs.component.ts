import { Component, OnInit } from '@angular/core';
import { AccountService } from '../../_services/account.service';
import { CommonModule, NgClass, NgFor, NgIf } from '@angular/common';
import { User } from '../../_models/user';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { PaginatedResult } from '../../_models/pagination';
import { ToastrService } from 'ngx-toastr';
import { ConfirmModalComponent } from '../../confirm-modal/confirm-modal.component';
import { OverlayModalService } from '../../_services/overlay-modal.service';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { UserFilterComponent } from '../../_filters/user-filter/user-filter.component';
import { GlobalLoaderService } from '../../_services/global-loader.service';

@Component({
  selector: 'app-list-utilisateurs',
  standalone: true,
  imports: [
    NgFor, NgIf, NgClass,
    FormsModule, RouterLink, CommonModule,
    MatMenuModule, MatIconModule, MatButtonModule,
    UserFilterComponent
  ],
  templateUrl: './list-utilisateurs.component.html',
  styleUrls: ['./list-utilisateurs.component.css']
})
export class ListUtilisateursComponent implements OnInit {
  currentRoleName!: string;
  private excludeRole?: string;

  pageNumber = 1;
  pageSize = 9;
  paginatedResult: PaginatedResult<User[]> | null = null;
  jumpPage = 1;
  usersSearchTerm = '';
  filterParams: any = {};
  newUserId: number | null = null;
  isExportLoading = false;
  isDeleteLoading = false;

  constructor(
    public accountService: AccountService,
    private route: ActivatedRoute,
    private router: Router,
    private toastr: ToastrService,
    private overlayModalService: OverlayModalService,
    private globalLoaderService: GlobalLoaderService
  ) {}

  ngOnInit(): void {
    this.route.data.subscribe(data => {
      if (data['excludeRole']) {
        this.excludeRole = data['excludeRole'];
        this.currentRoleName = 'Personnels';
        // on laisse service gérer tout, on filtrera après
        this.filterParams = {};
      } else {
        this.currentRoleName = data['role'];
        this.filterParams = { role: this.currentRoleName };
      }
      this.loadUsers();
    });

    this.route.queryParams.subscribe(params => {
      const newUser = params['newUser'];
      if (newUser) {
        this.newUserId = +newUser;
        setTimeout(() => (this.newUserId = null), 2000);
      }
    });
  }

  private loadUsers(): void {
    this.globalLoaderService.showGlobalLoader();
    const search = this.usersSearchTerm.trim();
    this.accountService
      .getUsers(this.pageNumber, this.pageSize, search, this.filterParams)
      .subscribe({
        next: response => {
          let items = (response.items ?? []).map(u => {
            if (u.contrat) {
              u.contrat.dateDebut = new Date(u.contrat.dateDebut);
              if (u.contrat.dateFin) {
                u.contrat.dateFin = new Date(u.contrat.dateFin);
              }
            }
            return { ...u, selected: false };
          });
          // **Si on a excludeRole, on retire ces rôles côté client**
          if (this.excludeRole) {
            items = items.filter(u => u.role.toLowerCase() !== this.excludeRole!.toLowerCase());
          }
          this.paginatedResult = {
            items,
            // ⚠️ Attention : si on retire des items on devrait mettre à jour pagination.totalItems
            pagination: response.pagination
          };
          this.accountService.paginatedResult.set(this.paginatedResult);
        },
        error: err => console.error('Erreur chargement utilisateurs', err),
        complete: () => this.globalLoaderService.hideGlobalLoader()
      });
  }

  onFilter(filterValues: any): void {
    // on reconstruit filterParams (role only pour Clients)
    if (!this.excludeRole) {
      this.filterParams = { role: this.currentRoleName, ...filterValues };
    } else {
      this.filterParams = { ...filterValues };
    }
    this.pageNumber = 1;
    this.loadUsers();
  }

  onSearchChange(): void {
    this.pageNumber = 1;
    this.loadUsers();
  }

  onPageChange(newPage: number): void {
    const maxPage = this.paginatedResult?.pagination?.totalPages ?? 1;
    this.pageNumber = Math.min(Math.max(newPage, 1), maxPage);
    this.jumpPage = this.pageNumber;
    this.loadUsers();
  }

  jumpToPage(): void {
    const total = this.paginatedResult?.pagination?.totalPages ?? 1;
    this.jumpPage = Math.min(Math.max(this.jumpPage, 1), total);
    this.onPageChange(this.jumpPage);
  }

  selectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const items = this.paginatedResult?.items ?? [];
    items.forEach(u => (u.selected = checked));
    this.accountService.paginatedResult.set({ ...this.paginatedResult, items });
  }

  toggleSelection(user: User): void {
    user.selected = !user.selected;
    const all = this.paginatedResult?.items?.every(u => u.selected) ?? false;
    const chk = document.getElementById('selectAll') as HTMLInputElement;
    if (chk) chk.checked = all;
    this.accountService.paginatedResult.set(this.paginatedResult!);
  }

  getRoleClass(role: string): string {
    switch (role.toLowerCase()) {
      case 'super admin':
        return 'super-admin';
      case 'chef de projet':
        return 'chef-de-projet';
      case 'développeur':
        return 'developpeur';
      case 'client':
        return 'client';
      default:
        return '';
    }
  }

  range(start: number, end: number): number[] {
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  deleteUser(user: User): void {
    const modal = this.overlayModalService.open(ConfirmModalComponent);
    modal.message = `Supprimer ${user.firstName} ${user.lastName} ?`;
    modal.confirmed.subscribe(() => {
      this.isDeleteLoading = true;
      this.accountService.deleteUser(user.id).subscribe({
        next: () => {
          this.toastr.success('Utilisateur supprimé');
          this.loadUsers();
        },
        error: e => console.error('Erreur suppression', e),
        complete: () => (this.isDeleteLoading = false)
      });
      this.overlayModalService.close();
    });
    modal.cancelled.subscribe(() => this.overlayModalService.close());
  }

  exportUsers(): void {
    this.isExportLoading = true;
    this.accountService
      .exportUsers(this.usersSearchTerm, this.filterParams)
      .subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `Users_${this.currentRoleName}_${new Date().toISOString()}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: e => console.error('Erreur export', e),
        complete: () => (this.isExportLoading = false)
      });
  }
}
