import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { BehaviorSubject, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { AppNotification } from '../_models/notification';
import { AccountService } from './account.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private hubConnection!: signalR.HubConnection;
  private notifications: AppNotification[] = [];
  public notifications$ = new BehaviorSubject<AppNotification[]>([]);
  public notification$  = new Subject<AppNotification>();

  constructor(
    private http: HttpClient,
    private account: AccountService
  ) {}

  /** Type guard pour distinguer User de ClientDto */
  private isUser(current: any): current is { id: number; role: string } {
    return current && 'role' in current && typeof current.role === 'string';
  }

  /** Détermine dynamiquement le contexte “users” vs “clients” */
  private get context() {
    const current = this.account.currentUser();
    if (!current || current.id == null) {
      throw new Error('Impossible de déterminer l’utilisateur courant');
    }

    const isUser = this.isUser(current);
    const key   = isUser ? 'user'   : 'client';
    const id    = current.id;   // commun aux deux
    return { key, id };
  }

  /** URL de base, ex. /api/clients/42/notifications/ */
  private get baseUrl() {
    const { key, id } = this.context;
    // on gère pluriels & minuscules
    return `${environment.apiUrl}${key}s/${id}/notifications`;
  }

  /** Démarre la connexion SignalR en adaptant le query string */
  public startConnection(): void {
    const { key, id } = this.context;
    const qs = `${key}Id=${id}`;
    if (this.hubConnection) {
      // Si on n’est pas complètement arrêté, on ne fait rien
      const st = this.hubConnection.state;
      if (st !== signalR.HubConnectionState.Disconnected) {
        console.log(`SignalR déjà en état ${st}, on ne redémarre pas.`);
        return;
      }
      // OK, on était Disconnected : on peut cleanup l’ancienne connexion
      this.hubConnection.off('ReceiveNotification');
      // on peut éventuellement attendre this.hubConnection.stop()
    }
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRHubUrl}?${qs}`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (dto: AppNotification) => {
      // ignore si pas pour nous
      if ((dto.userId  && dto.userId !== this.context.id)
       || (dto.clientId && dto.clientId !== this.context.id)) {
        return;
      }
      if (this.notifications.find(n => n.id === dto.id)) return;
      dto.isRead = false;
      this.notifications.unshift(dto);
      this.notifications$.next(this.notifications);
      this.notification$.next(dto);
    });

    this.hubConnection.start()
      .then(() => console.log('SignalR connecté'))
      .catch(err => console.error('SignalR erreur', err));
  }


  public getNotifications() {
    return this.http
      .get<AppNotification[]>(`${this.baseUrl}`)
      .pipe(tap(list => {
        this.notifications = list;
        this.notifications$.next(list);
      }));
  }

  public markAllAsRead() {
    return this.http.post<void>(`${this.baseUrl}/markasread`, {});
  }


  public markAsRead(id: number) {
    return this.http.post<void>(`${this.baseUrl}/${id}/markasread`, {});
  }

  public hideNotification(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`, {});
  }
}

