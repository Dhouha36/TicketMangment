import { Component, OnInit, NgZone, HostListener, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AccountService } from '../_services/account.service';
import { SidenavService } from '../_services/sideNavService.service';
import { User } from '../_models/user';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { SearchResultDTO, SearchService } from '../_services/search.service';
import { FormsModule } from '@angular/forms';
import { NotificationsComponent } from '../notifications/notifications.component';
import { NotificationService } from '../_services/notification.service';
import { AppNotification } from '../_models/notification';
import { ClientDto } from '../DTOs/ClientDto';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule,
    NotificationsComponent
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
  currentUser: User | ClientDto | null = null;
  userInitials = "";
  isMenuOpen: boolean = false;

  query: string = '';
  results: SearchResultDTO[] = [];
  private searchSubject = new Subject<string>();
  isSearchActive: boolean = false;
  @ViewChild('searchBar') searchBar!: ElementRef;

  notifications: AppNotification[] = [];
  isNotifOpen = false;
  unreadCount = 0;

  constructor(
    private accountService: AccountService,
    private router: Router,
    private ngZone: NgZone,
    private sidenavService: SidenavService,
    private searchService: SearchService,
    private notifSvc: NotificationService,
    private elementRef: ElementRef
  ) {
    // Débouncer la recherche pour éviter des appels API trop fréquents
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(q => this.executeSearch(q));
  }

  private isUser(u: User | ClientDto | null): u is User {
    return !!u && (u as User).role !== undefined;
  }

  /** True si c’est un vrai client en base (ClientDto, sans `role`) */
  private isClientDto(u: User | ClientDto | null): u is ClientDto {
    return !!u && (u as ClientDto).paysId !== undefined;
  }


  ngOnInit(): void {
    this.currentUser = this.accountService.currentUser();
    if (this.currentUser) {
      this.userInitials =
        this.currentUser.firstName.charAt(0).toUpperCase() +
        this.currentUser.lastName.charAt(0).toUpperCase();
    }

    const user = this.accountService.currentUser();
    if (user?.id) {
      const uid = user.id.toString();
      this.notifSvc.startConnection(user.id.toString());
      // Charge l’historique, compte seulement les non-lues 
      this.notifSvc.getNotifications(uid)
        .subscribe(notifs => {
          this.notifications = notifs;
          this.unreadCount = notifs.filter(n => !n.isRead).length;
        });

      // En temps réel, on crée la notification avec isRead=false
      this.notifSvc.notification$.subscribe((dto: AppNotification) => {
        this.notifications.unshift(dto);
        this.unreadCount++;
      });
    }
  }

  toggleNotifications(): void {
    this.isNotifOpen = !this.isNotifOpen;
    if (!this.isNotifOpen && this.notifications.length) {
      // on marque en lu seulement à la fermeture
      const uid = this.accountService.currentUser()!.id.toString();
      this.notifSvc.markAllAsRead(uid).subscribe(() => {
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
      });
    }
  }
  

  toggleSidenav() {
    this.sidenavService.toggleSidenav();
  }

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
  }

  // Méthode pour naviguer vers le profil utilisateur
  navigateToProfile(): void {
    // Remplacez '/profile' par la route correspondante à votre profil
    this.router.navigate(['home/profile']);
    this.isMenuOpen = false; // Ferme le menu après navigation
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

  onSearchChange(): void {
    this.searchSubject.next(this.query);
  }


  executeSearch(query: string): void {
    if (!query) {
      this.results = [];
      return;
    }
    this.searchService.search(query).subscribe(data => {
      this.results = data;
    });
    this.isSearchActive = this.query.length > 0;
  }

  // Redirige l'utilisateur selon le type du résultat cliqué
  navigateToResult(result: SearchResultDTO): void {
    let route: string[];
    const queryParams = { t: new Date().getTime().toString() };

    switch (result.type) {
      case 'User':
        route = ['/home/utilisateurs/details', result.id.toString()];
        break;
      case 'Projet':
        route = ['/home/Projets/details', result.id.toString()];
        break;
      case 'Societe':
        route = ['/home/Societes/modifierSociete', result.id.toString()];
        break;
      case 'Role':
        route = ['/home/utilisateurs/details', result.id.toString()];
        break;
      case 'Ticket':
      case 'Qualification':
      case 'Priorite':
      case 'CategorieProbleme':
      case 'StatutDesTicket':
      case 'Commentaire':
        route = ['/home/Tickets/details', result.id.toString()];
        break;
      case 'Pays':
        route = ['home/Pays/ModifierPays', result.id.toString()];
        break;
      case 'ContratSociete':
        route = ['/home/Societes/modifierSociete', result.id.toString()];
        break;
      case 'ContratUser':
        route = ['/home/utilisateurs/details', result.id.toString()];
        break;
      default:
        return;
    }

    this.router.navigate(route, {
      queryParams: queryParams,
      queryParamsHandling: 'merge' // 🛠️ Permet d'ajouter un paramètre pour forcer le changement
    }).then(() => {
      this.query = '';
      this.results = [];
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const clickedInside = this.elementRef.nativeElement.contains(event.target);
    if (!clickedInside) {
      this.isMenuOpen = false;
      this.isSearchActive = false;
      this.isNotifOpen = false;
    }
  }

  get displayRole(): string {
    if (this.isUser(this.currentUser)) {
      return this.currentUser.role;          // ex. "Super Admin"
    }
    if (this.isClientDto(this.currentUser)) {
      return 'Client';                       // rôle statique pour les clients
    }
    return '';
  }


}
