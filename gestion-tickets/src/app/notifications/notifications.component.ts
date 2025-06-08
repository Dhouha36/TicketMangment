import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { NotificationService } from '../_services/notification.service';
import { AppNotification } from '../_models/notification';
import { Router } from '@angular/router';
import { ConfirmModalComponent } from '../confirm-modal/confirm-modal.component';
import { OverlayModalService } from '../_services/overlay-modal.service';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css'
})
export class NotificationsComponent implements OnInit {
  @Output() closeSidenav = new EventEmitter<void>()
  notifications: AppNotification[] = [];
  userId!: string;

  showConfirmModal = false;
  confirmMessage = 'Voulez-vous vraiment supprimer cette notification ?';
  private notificationToDelete?: AppNotification;

  constructor(
    private notifSvc: NotificationService,
    private router: Router,
    private overlayModal: OverlayModalService
  ) { }

  ngOnInit(): void {
    const storedUser = localStorage.getItem('user');
    if (!storedUser) { return; }
    this.userId = JSON.parse(storedUser).id.toString();

    // Historique + fusion
    this.notifSvc.getNotifications(this.userId).subscribe({
      error: err => console.error('Erreur chargement notifs', err)
    });

    // Affichage : on se met à jour quand la liste change
    this.notifSvc.notifications$.subscribe(list => {
      this.notifications = list;
    });
  }

  markAllAsRead(): void {
    this.notifSvc.markAllAsRead(this.userId).subscribe(() => {
      this.notifications.forEach(n => n.isRead = true);
    });
  }

  markAsRead(n: AppNotification): void {
    if (n.isRead) return;
    this.notifSvc.markAsRead(n.id).subscribe(() => {
      n.isRead = true;
    });
  }

  goTo(n: AppNotification) {
    // 1) Si pas de type, on sort
    if (!n.entityType) {
      console.warn('Notification sans entityType, on ne navigue pas', n);
      return;
    }

    // 2) Mapping entityType → segment de route
    const routeMap: Record<string, string> = {
      Projets: 'Projets',
      Societe: 'Societes',
      Tickets: 'Tickets',
    };
    const segment = routeMap[n.entityType];
    if (!segment) {
      console.warn(`Pas de route configurée pour entityType='${n.entityType}'`);
      return;
    }

    // 3) Construction du tableau de segments
    const commands = n.entityId
      ? ['/home', segment, 'details', n.entityId.toString()]
      : ['/home', segment];

    // 4) Vérification si on est déjà sur cette URL exacte
    const targetTree = this.router.createUrlTree(commands);
    if (this.router.isActive(targetTree, true)) {
      // On marque en lu et on ferme le sidenav sans naviguer
      this.markAsRead(n);
      this.closeSidenav.emit();
      return;
    }

    // 5) Navigation si on n’était pas déjà dessus
    this.router.navigate(commands).then(success => {
      if (success) {
        this.markAsRead(n);
        this.closeSidenav.emit();
      }
    });
  }

  openConfirm(n: AppNotification): void {
    // 1) Ouvre le ConfirmModalComponent dans un overlay centré
    const modal = this.overlayModal.open(ConfirmModalComponent);

    // 2) Personnalise le texte
    modal.message = 'Voulez-vous vraiment supprimer cette notification ?';

    // 3) Si l’utilisateur confirme
    const subConf = modal.confirmed.subscribe(() => {
      this.hideNotification(n);
      subConf.unsubscribe();
      this.overlayModal.close();
    });

    // 4) Si l’utilisateur annule
    const subCancel = modal.cancelled.subscribe(() => {
      subCancel.unsubscribe();
      this.overlayModal.close();
    });
  }

  private hideNotification(n: AppNotification) {
    this.notifSvc.hideNotification(n.id).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(x => x.id !== n.id);
        this.notifSvc.getNotifications(this.userId).subscribe(); // Rafraîchissement
      },
      error: err => console.error('Erreur masquage notification', err)
    });
  }

}
