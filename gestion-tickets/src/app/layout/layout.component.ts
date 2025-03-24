import { Component, OnDestroy, OnInit } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Subscription } from 'rxjs';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { SidenavService } from '../_services/sideNavService.service';
import { AccountService } from '../_services/account.service';
import { User } from '../_models/user';
import { SignalRService } from '../_services/signalR.service';
import { AppNotification } from '../_models/app-notification';
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
  styleUrls: ['./layout.component.css']
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
  notifications: AppNotification[] = [];

  // Autres variables
  isModalOpen = false;
  userInitials = "";

  constructor(
    private sidenavService: SidenavService,
    private breakpointObserver: BreakpointObserver,
    public route: ActivatedRoute,
    private accountService: AccountService,
    private signalRService: SignalRService
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

    // Abonnement aux notifications via SignalR
    this.signalRService.notificationReceived
      .pipe(filter(notification => notification !== null))
      .subscribe((notification: AppNotification) => {
        // On ajoute la notification au début du tableau
        this.notifications.unshift(notification);
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
  get currentUser(): User | null {
    return this.accountService.currentUser();
  }

  isSuperAdmin(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'super admin';
  }
  isChefDeProjet(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'chef de projet';
  }
  isCollaborateur(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'collaborateur';
  }
  isClient(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'client';
  }

  // Méthodes pour le sidenav des notifications
  toggleNotificationSidenav(): void {
    this.isNotificationSidenavOpen = !this.isNotificationSidenavOpen;
  }

  markAllAsRead(): void {
    this.notifications.forEach(n => n.estLu = true);
  }

  clearNotifications(): void {
    this.notifications = [];
  }

  removeNotification(id: number): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }
}
