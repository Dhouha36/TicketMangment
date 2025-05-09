import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { AccountService } from './_services/account.service';
import { AngularEditorModule } from '@kolkov/angular-editor';
import { GlobalLoaderComponent } from './global-loader/global-loader.component';
import { NotificationService } from './_services/notification.service';
import { PushSubscriptionService } from './_services/push-subscription.service';

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
  private router           = inject(Router);
  private pushSubService   = inject(PushSubscriptionService);
  private notificationService = inject(NotificationService);

  async ensurePermission(): Promise<boolean> {
    if (Notification.permission === 'granted') return true;
    const perm = await Notification.requestPermission();
    return perm === 'granted';
  }

  ngOnInit(): void {
    this.setCurrentUser();

    if ('serviceWorker' in navigator) {
      console.log('SW controller:', navigator.serviceWorker.controller);
      navigator.serviceWorker.ready
        .then(reg => console.log('SW ready, scope=', reg.scope));
    }
    // 1) Récupérer l’ID utilisateur (ex. via localStorage ou token)
    const user = JSON.parse(localStorage.getItem('user') || 'null');
    if (user && user.id) {
      const userIdStr = user.id.toString();

      this.ensurePermission().then(granted => {
        if (!granted) {
          console.warn('Push notifications non autorisées par l’utilisateur');
          return;
        }
        // ensuite : abonnement et SignalR…
        this.pushSubService.subscribeToPush(userIdStr);
        this.notificationService.startConnection(userIdStr);
      });

      // 4) Écouter les notifications entrantes
      this.notificationService.notification$.subscribe(msg => {
        console.log('Notification reçue:', msg);
        // Vous pouvez aussi afficher un toast ici via Toastr, etc.
      });
    }
  }

  private setCurrentUser() {
    const userString = localStorage.getItem('user');
    if (!userString) return;
    const user = JSON.parse(userString);
    this.accountService.currentUser.set(user);

    // Valider le token avec le backend
    setTimeout(() => {
      this.accountService.validateToken().subscribe({
        error: () => {
          this.accountService.logout();
          this.router.navigate(['/login']);
        }
      });
    }, 0);
  }
}
