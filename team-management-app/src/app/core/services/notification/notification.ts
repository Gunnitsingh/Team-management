import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../contants/constants';
import { SignalRNotification, TaskProjectionMessage } from './notification.interface';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private connection?: signalR.HubConnection;
  private readonly snackBar = inject(MatSnackBar);
  private readonly notificationsSubject = new Subject<SignalRNotification>();
  private readonly taskProjectionSubject = new Subject<TaskProjectionMessage>();

  public readonly notifications$ = this.notificationsSubject.asObservable();
  public readonly taskProjectionUpdates$ = this.taskProjectionSubject.asObservable();

  start() {
    if (this.connection) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api', '')}/notificationHub`, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this.connection.start()
      .catch(err => console.log(err));

    this.connection.on('ReceiveNotification', (data: SignalRNotification) => {
      this.notificationsSubject.next(data);
      this.snackBar.open(data.title, data.message, { duration: 2000 });
    });

    this.connection.on('TaskProjectionUpdated', (data: TaskProjectionMessage) => {
      this.taskProjectionSubject.next(data);
    });
  }
}
