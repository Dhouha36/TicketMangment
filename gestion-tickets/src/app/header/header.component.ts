import { Component, EventEmitter, NgZone, OnInit, Output } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Router } from '@angular/router';
import { SidenavService } from '../_services/sideNavService.service';
import { SignalRService } from '../_services/signalR.service';
import { AppNotification } from '../_models/app-notification';
import { User } from '../_models/user';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
  // Événement déclenché lors du clic sur l'icône de notifications
  @Output() toggleNotifications = new EventEmitter<void>();
  currentUser: User | null = null;
  userInitials = "";


  constructor(
    private accountService: AccountService,
    private router: Router,
    private signalRService: SignalRService,
    private ngZone: NgZone,
    private sidenavService: SidenavService
  ) { }

  ngOnInit(): void {
    this.currentUser = this.accountService.currentUser();
    if (this.currentUser) {
      this.userInitials =
        this.currentUser.firstName.charAt(0).toUpperCase() +
        this.currentUser.lastName.charAt(0).toUpperCase();
    }
  }

toggleSidenav() {
  this.sidenavService.toggleSidenav();
}

// Méthode appelée lors du clic sur l'icône de notifications
onNotificationClick(): void {
  this.toggleNotifications.emit();
}

logout() {
  this.accountService.logout();
  this.router.navigateByUrl('/');
}
}
