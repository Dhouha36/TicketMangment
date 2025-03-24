import { AccountService } from './account.service';
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';
import { AppNotification } from '../_models/app-notification';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  // Déclarez la variable avec le type AppNotification
  public notificationReceived = new BehaviorSubject<AppNotification | null>(null);
  private hubConnection: signalR.HubConnection;


  constructor(private accountService: AccountService) {
    const hubUrl = `${environment.apiUrl.replace('/api', '')}/notificationHub`;
    console.log('Tentative de connexion au hub :', hubUrl);

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.accountService.currentUser()?.token || ""
      })
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: AppNotification) => {
      console.log("Notification reçue :", notification);
      this.notificationReceived.next(notification);
    });

    this.startConnection();
  }


  private startConnection() {
    this.hubConnection.start()
      .then(() => console.log('Connexion SignalR établie.'))
      .catch(err => console.error('Erreur lors de la connexion SignalR :', err));

    this.hubConnection.onclose(error => {
      console.error('La connexion SignalR a été fermée', error);
    });
  }
}
