import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { firstValueFrom } from 'rxjs';

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64  = (base64String + padding)
    .replace(/\-/g, '+')
    .replace(/_/g, '/');
  const rawData = window.atob(base64);
  return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)));
}

@Injectable({ providedIn: 'root' })
export class PushSubscriptionService {
  constructor(private http: HttpClient) {}

  public async subscribeToPush(userId: string): Promise<void> {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      console.warn('Push non supporté');
      return;
    }

    if (Notification.permission !== 'granted') {
      console.warn('Notifications non autorisées');
    }
    
    const reg = await navigator.serviceWorker.ready;
    const sub = await reg.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(environment.vapidPublicKey)
    });

    try {
      await firstValueFrom(this.http.post(
        `${environment.apiUrl}push/subscribe`,
        { userId, subscription: sub.toJSON() }
      ));
      console.log('Abonnement push enregistré côté serveur');
    } catch (err) {
      console.error('Échec de l’abonnement push :', err);
    }
  }
}