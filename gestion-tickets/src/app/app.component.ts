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


  public async ngOnInit(): Promise<void> {
    // 1) Charger l’utilisateur à jour
    await this.loadCurrentUserFromApi();

    // 2) Validation du token
    const current = this.accountService.currentUser();
  if (current?.token) {
    // On est connecté → validation du token possible
    this.accountService.validateToken().subscribe({
      error: () => {
        this.accountService.logout();
        this.router.navigate(['/login']);
      }
    });
  }
    // 3) Enregistrer le Service Worker
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker
        .register('/sw.js')
        .then(reg => console.log('Service Worker enregistré, scope:', reg.scope))
        .catch(err => console.error('Erreur enregistrement SW:', err));
    }

    // 4) Démarrer SignalR et Push
    if (current?.id != null) {
      // 4a) SignalR temps réel
      this.notificationService.startConnection();

      // 4b) Push notifications
      const granted = await this.ensurePermission();
      if (granted) {
        try {
          await this.pushSubService.subscribeToPush(current.id.toString());
        } catch (err) {
          console.error('Erreur lors de l’abonnement Push :', err);
        }
      } else {
        console.warn('Push notifications non autorisées');
      }

      // 4c) Écoute pour toasts in‑app
      this.notificationService.notification$.subscribe(dto => {
        console.log('Notification reçue en temps réel :', dto);
      });
    }
  }
}
