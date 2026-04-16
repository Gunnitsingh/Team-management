import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import * as signalR from '@microsoft/signalr';
import { SignalRNotification } from './notification.interface';

export class NotificationService {
  private connection!: signalR.HubConnection;
  private readonly snackBar = inject(MatSnackBar);

  start() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:8080/notificationHub', {
        withCredentials: true // default usually, but ok to be explicit
      })
      .withAutomaticReconnect()
      .build();

    this.connection.start()
      .catch(err => console.log(err));

    this.connection.on('ReceiveNotification', (data: SignalRNotification) => {
      this.snackBar.open(data.title, data.message, { duration: 2000 });
    });
  }
}
