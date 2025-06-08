import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { AccountService } from './_services/account.service';
import { AngularEditorModule } from '@kolkov/angular-editor';
import { GlobalLoaderComponent } from './global-loader/global-loader.component';
import { NotificationService } from './_services/notification.service';
import { PushSubscriptionService } from './_services/push-subscription.service';
import { ClientDto } from './DTOs/ClientDto';
import { User } from './_models/user';
import { firstValueFrom } from 'rxjs';
import { ClientService } from './_services/client.service';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    AngularEditorModule,
    GlobalLoaderComponent,
],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  private accountService   = inject(AccountService);
  private clientService    = inject(ClientService);
  private router           = inject(Router);
  private pushSubService   = inject(PushSubscriptionService);
  private notificationService = inject(NotificationService);

  async ensurePermission(): Promise<boolean> {
    if (Notification.permission === 'granted') return true;
    const perm = await Notification.requestPermission();
    return perm === 'granted';
  }

  private async loadCurrentUserFromApi(): Promise<void> {
    const userString = localStorage.getItem('user');
    if (!userString) {
      return;
    }

    // 1) On récupère la version “stale” pour en extraire id (+ savoir si c’est un User ou un Client)
    const parsed: any = JSON.parse(userString);
    if (!parsed.id) {
      return;
    }

    const userId = Number(parsed.id);
    const isClient = parsed.role === undefined; 
    // → si parsed.role est undefined, on suppose qu’il s’agit d’un ClientDto

    try {
      if (isClient) {
        // 2a) Récupérer le ClientDto à jour
        const freshClient: ClientDto = await firstValueFrom(this.clientService.getById(userId));
        // 3a) Mettre à jour le signal + localStorage
        this.accountService.setCurrentUser(freshClient);
        localStorage.setItem('user', JSON.stringify(freshClient));
      } else {
        // 2b) Récupérer le User à jour
        const freshUser: User = await firstValueFrom(this.accountService.getUser(userId));
        // 3b) Mettre à jour le signal + localStorage
        this.accountService.setCurrentUser(freshUser);
        localStorage.setItem('user', JSON.stringify(freshUser));
      }
    } catch (err) {
      // Si l’API renvoie une erreur (token invalide, user non trouvé, etc.), on nettoie la session
      console.error('Impossible de charger l’utilisateur depuis l’API', err);
      this.accountService.logout();
      this.router.navigate(['/login']);
    }
  }


  async ngOnInit(): Promise<void> {
    // 1) On va charger la version à jour de l’utilisateur avant toute autre logique
    await this.loadCurrentUserFromApi();

    // 2) Valider le token (s’il est encore valide) et forcer logout si besoin
    setTimeout(() => {
      this.accountService.validateToken().subscribe({
        error: () => {
          this.accountService.logout();
          this.router.navigate(['/login']);
        }
      });
    }, 0);

    // 3) Mise en place du Service Worker (optionnel)
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.ready.then(reg => 
        console.log('SW ready, scope=', reg.scope)
      );
    }

    // 4) Démarrer la connexion aux notifications (WebSocket ou SignalR)
    const current = this.accountService.currentUser();
    if (current && current.id) {
      const uid = current.id.toString();
      this.notificationService.startConnection(uid);

      // 5) Demander la permission pour les notifications push
      const granted = await this.ensurePermission();
      if (granted) {
        this.pushSubService.subscribeToPush(uid);
      } else {
        console.warn('Push notifications non autorisées');
      }

      // 6) S’abonner aux notifications reçues en temps réel
      this.notificationService.notification$.subscribe(msg => {
        //console.log('Notification reçue:', msg);
        // Ici vous pourriez déclencher un toast, etc.
      });
    }
  }
}
