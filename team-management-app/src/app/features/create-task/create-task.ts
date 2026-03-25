import { Component, computed, effect, inject, input, model, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogActions, MatDialogClose, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Observable } from 'rxjs';
import { UsersService } from '../../core/services/users/users';
import { Users } from '../../core/services/users/users.interface';
import { CommonModule } from '@angular/common';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, provideNativeDateAdapter } from '@angular/material/core';
import { Task } from '../../core/services/tasks/task-interface';

@Component({
  selector: 'app-create-task',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatDialogModule,
    CommonModule, MatDialogClose, MatDatepickerModule, MatNativeDateModule],
  providers: [provideNativeDateAdapter()],
  templateUrl: './create-task.html',
  styleUrls: ['./create-task.css'],
  standalone: true
})



export class CreateTaskComponent implements OnInit {

  public data = inject<CreateTaskDialogData>(MAT_DIALOG_DATA);

  ngOnInit(): void {


    if (this.data.task) {
      const task = this.data.task;
      this.taskForm.patchValue({
        title: task.title,
        description: task.description,
        priority: task.priority,
        assignedTo: task.assignedToId,
        dueDate: task.dueDate
      });
    }

    this.taskForm.valueChanges.subscribe((value) => {
      console.log(value)
    })
  }
  private readonly formBuilder = inject(FormBuilder);
  public priorities = ["Low", "Medium", "High", "Critical", "Blocker"]

  public taskForm = this.formBuilder.group({
    title: ['', Validators.required],
    description: ['', Validators.required],
    priority: ['Low'],
    assignedTo: [0],
    dueDate: [new Date()],
  });
  readonly formData = model(this.taskForm);

}
export interface CreateTaskDialogData {
  title: string;
  task: Task
  users: Observable<Users[]>
}