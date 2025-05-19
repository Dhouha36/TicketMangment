import { Component, OnDestroy, OnInit } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Subscription } from 'rxjs';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { SidenavService } from '../_services/sideNavService.service';
import { AccountService } from '../_services/account.service';
import { User } from '../_models/user';
import { filter } from 'rxjs/operators';
import { HeaderComponent } from '../header/header.component';
import { CommonModule, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { ClientDto } from '../DTOs/ClientDto';

@Component({
  selector: 'app-layout',
  imports: [
    HeaderComponent,
    RouterModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    FormsModule,
    NgIf,
    CommonModule
  ],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent implements OnInit, OnDestroy {

  // Pour le menu principal (gauche)
  isSidenavOpen: boolean = true;
  sidenavMode: 'side' | 'over' = 'side';
  private sidenavSubscription?: Subscription;

  // Pour le menu déroulant dans le menu principal
  isBaseDataOpen = false;
  isAccountOpen = false;

  // Pour les notifications (sidenav droit)
  isNotificationSidenavOpen: boolean = false;

  // Autres variables
  isModalOpen = false;
  userInitials = "";

  constructor(
    private sidenavService: SidenavService,
    private breakpointObserver: BreakpointObserver,
    public route: ActivatedRoute,
    private accountService: AccountService,
  ) { }

  ngOnInit() {
    if (this.currentUser) {
      this.userInitials = this.currentUser.firstName.charAt(0) + this.currentUser.lastName.charAt(0);
    }

    // Abonnement au sidenav principal (géré par le service)
    this.sidenavSubscription = this.sidenavService.sidenavState$.subscribe(state => {
      this.isSidenavOpen = state;
    });

    // Détecter la taille de l'écran pour adapter le mode du sidenav principal
    this.breakpointObserver.observe([Breakpoints.Small, Breakpoints.XSmall])
      .subscribe(result => {
        if (result.matches) {
          this.sidenavMode = 'over';
          this.sidenavService.setSidenavState(false);
        } else {
          this.sidenavMode = 'side';
          this.sidenavService.setSidenavState(true);
        }
      });

  }

  ngOnDestroy() {
    if (this.sidenavSubscription) {
      this.sidenavSubscription.unsubscribe();
    }
  }

  // Méthodes pour le menu principal
  toggleBaseData() {
    this.isBaseDataOpen = !this.isBaseDataOpen;
  }
  toggleAccount() {
    this.isAccountOpen = !this.isAccountOpen;
  }

  // Méthode pour récupérer l'utilisateur courant
  get currentUser(): User | ClientDto | null {
    return this.accountService.currentUser();
  }

  isSuperAdmin(): boolean {
    const u = this.currentUser;
    if (!u || !('role' in u)) return false;
    return u.role.toLowerCase() === 'super admin';
  }

  /** Type‑guard pour détecter un User (interne) */
  private isUser(u: User | ClientDto | null): u is User {
    return !!u && (u as User).role !== undefined;
  }

  /** Type‑guard pour détecter un ClientDto (client réel en base) */
  private isClientDto(u: User | ClientDto | null): u is ClientDto {
    return !!u && (u as ClientDto).paysId !== undefined;
  }

  isChefDeProjet(): boolean {
    if (!this.isUser(this.currentUser)) {
      return false;  // ce n’est pas un User, donc pas Chef de Projet
    }
    return this.currentUser.role.toLowerCase() === 'chef de projet';
  }

  isCollaborateur(): boolean {
    if (!this.isUser(this.currentUser)) {
      return false;  // ce n’est pas un User, donc pas Collaborateur
    }
    return this.currentUser.role.toLowerCase() === 'collaborateur';
  }

  /**
   * L’utilisateur est « client » si et seulement si
   * il s’agit d’un ClientDto (venant de la table clients).
   */
  isClient(): boolean {
    return this.isClientDto(this.currentUser);
  }

  // Méthodes pour le sidenav des notifications
  toggleNotificationSidenav(): void {
    this.isNotificationSidenavOpen = !this.isNotificationSidenavOpen;
  }


}