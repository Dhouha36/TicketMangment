// push-notification.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {

  // Remplacez par votre clé VAPID publique générée
  private vapidPublicKey = 'BEEZtKVh995Du3gHeCl_b80WUS6-b3D6gfqEj5IdKJTNYKkeERqBKgWbi_3I9ObOCdpdG7zr1KkUjHGiePRX0GM';

  constructor(private http: HttpClient) { }

  // Demande la permission et s'abonne
  public subscribeToPushNotifications() {
    // Demande de permission
    Notification.requestPermission().then(permission => {
      if (permission !== 'granted') {
        console.error('Permission de notification refusée');
        return;
      }
      // Accéder au Service Worker et s'abonner
      navigator.serviceWorker.ready.then(registration => {
        const convertedVapidKey = this.urlBase64ToUint8Array(this.vapidPublicKey);
        registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: convertedVapidKey
        })
        .then(subscription => {
          console.log('Utilisateur abonné :', subscription);
          // Envoyer l'objet d'abonnement au serveur
          this.sendSubscriptionToServer(subscription);
        })
        .catch(err => console.error('Erreur lors de l\'abonnement :', err));
      });
    });
  }

  // Conversion de la clé VAPID depuis la base64
  private urlBase64ToUint8Array(base64String: string) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
      .replace(/\-/g, '+')
      .replace(/_/g, '/');
    const rawData = window.atob(base64);
    return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)));
  }

  // Envoie de l'abonnement au backend via une requête HTTP
  private sendSubscriptionToServer(subscription: PushSubscription) {
    this.http.post(`${environment.apiUrl}/push/subscribe`, subscription)
      .subscribe(
        () => console.log('Abonnement enregistré sur le serveur.'),
        err => console.error('Erreur lors de l\'envoi de l\'abonnement au serveur :', err)
      );
  }
}
