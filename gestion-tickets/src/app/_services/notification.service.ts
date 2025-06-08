import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { BehaviorSubject, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { AppNotification } from '../_models/notification';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection!: signalR.HubConnection;

  // 1) buffer liste
  private notifications: AppNotification[] = [];
  public notifications$ = new BehaviorSubject<AppNotification[]>([]);

  // 2) flux individuel (remplace l’ancien notification$)
  public notification$ = new Subject<AppNotification>();

  private baseUrl = `${environment.apiUrl}notifications/`;
  private userId!: string;

  constructor(private http: HttpClient) { }

  public startConnection(userId: string): void {
    this.userId = userId;
  
    // Si hubConnection existe et qu’elle est déjà connectée ou en cours de reconnexion,
    // on ne relance pas une nouvelle instance.
    if (this.hubConnection &&
        (this.hubConnection.state === signalR.HubConnectionState.Connected
         || this.hubConnection.state === signalR.HubConnectionState.Reconnecting)) {
      //console.log('SignalR déjà connecté ou en cours de reconnexion, on ne relance pas.');
      return;
    }
  
    // Si hubConnection existe mais est en état "Disconnected", on arrête la connexion précédente
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Disconnected) {
      this.hubConnection.off('ReceiveNotification');
      // Note : .stop() est asynchrone mais on ne bloque pas ici
      this.hubConnection.stop().catch(err => console.error('Erreur arrêt SignalR :', err));
    }
  
    // On (re)crée la connexion
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRHubUrl}?userId=${userId}`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();
  
    this.hubConnection.on('ReceiveNotification', (dto: AppNotification) => {

  
      if (dto.userId !== +this.userId) {
        console.warn('Notification ignorée (userId incorrect)', dto);
        return;
      }
  
      dto.isRead = false;
      if (this.notifications.some(n => n.id === dto.id)) return;
  
      this.notifications.unshift(dto);
      this.notifications$.next(this.notifications);
      this.notification$.next(dto);
    });
  
    this.hubConnection
      .start()
      .then(() => console.log('SignalR connecté'))
      .catch(err => console.error('Erreur SignalR :', err));
  }
  

  public getNotifications(userId: string) {
    return this.http
      .get<AppNotification[]>(`${this.baseUrl}user/${userId}`)
      .pipe(
        tap(list => {
          this.notifications = list;
          this.notifications$.next(this.notifications);
        })
      );
  }

  markAllAsRead(userId: string) {
    return this.http.post<void>(`${this.baseUrl}markasread/${userId}`, {});
  }

  markAsRead(id: number) {
    return this.http.post<void>(`${this.baseUrl}markasread/one/${id}`, {});
  }

  hideNotification(id: number) {
    return this.http.post<void>(`${this.baseUrl}hide/${id}`, {});
  }
}
