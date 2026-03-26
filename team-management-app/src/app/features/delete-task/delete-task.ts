import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogClose, MatDialogModule } from '@angular/material/dialog';
import { Task } from '../../core/services/tasks/task-interface';

@Component({
  selector: 'app-delete-task',
  imports: [MatButtonModule,MatDialogModule, MatDialogClose],
  templateUrl: './delete-task.html',
  styleUrl: './delete-task.css',
})
export class DeleteTask {
   public data = inject<DeleteTaskInterface>(MAT_DIALOG_DATA);

}

export interface DeleteTaskInterface {
  task:Task
}