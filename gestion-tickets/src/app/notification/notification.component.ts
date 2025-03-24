import { Component, NgZone, OnInit } from '@angular/core';
import { SignalRService } from '../_services/signalR.service';
import { CommonModule } from '@angular/common';
import { AppNotification } from '../_models/app-notification';

@Component({
  selector: 'app-notification',
  imports: [ CommonModule],
  templateUrl: './notification.component.html',
  styleUrl: './notification.component.css'
})
export class NotificationComponent implements OnInit {
  notification: string = '';

  constructor(private signalRService: SignalRService, private ngZone: NgZone) {}

  ngOnInit(): void {
    // Souscrire au flux de notifications depuis le service SignalR
    this.signalRService.notificationReceived.subscribe((notification: AppNotification | null) => {
      console.log("Notification reçue dans le composant :", notification!.message);
      // Mise à jour dans la zone Angular pour déclencher la détection de changements
      this.ngZone.run(() => {
        this.notification = notification!.message;
      });
    });
  }
}
