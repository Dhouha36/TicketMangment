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

  constructor(private http: HttpClient) { }

  public startConnection(userId: string): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRHubUrl}?userId=${userId}`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

      this.hubConnection.on('ReceiveNotification', (dto: AppNotification) => {
        console.log('[SignalR] reçu:', dto);
        dto.isRead = false;
        console.log('[SignalR] forcé isRead →', dto.isRead);
        // 1) si on a déjà cet id, on ignore
        if (this.notifications.some(n => n.id === dto.id)) {
          return;
        }
        
      
        // 2) sinon on ajoute
        this.notifications.unshift(dto);
        this.notifications$.next(this.notifications);
      
        // 3) et on émet aussi sur le flux individuel
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
