import { Component, inject, OnInit } from '@angular/core';
import { CreateTaskInterface, Task } from '../../core/services/tasks/task-interface';
import { TaskService } from '../../core/services/tasks/task.service';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, filter, map, Observable, switchMap, take, tap } from 'rxjs';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { kanbanColumns, TASK_STATUS_LABELS, TaskStatus } from '../../contants/constants';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CreateTaskComponent } from '../create-task/create-task';
import { Users } from '../../core/services/users/users.interface';
import { UsersService } from '../../core/services/users/users';

@Component({
  selector: 'app-kanban-board',
  imports: [CommonModule, DragDropModule, MatButtonModule, MatDialogModule],
  templateUrl: './kanban-board.html',
  styleUrls: ['./kanban-board.css'],
  standalone: true
})
export class KanbanBoard implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  private readonly userService = inject(UsersService);
  public readonly columns = Object.values(TaskStatus);
  public users$: Observable<Users[]> = this.userService.getAllUsers()
  private tasksSubject = new BehaviorSubject<Task[]>([]);
  public taskStatusLabels = TASK_STATUS_LABELS;

  public tasksByStatus$: Observable<Record<string, Task[]>> = this.tasksSubject.pipe(
    map(tasks => this.columns.reduce((acc, c) => ({ ...acc, [c]: tasks.filter(t => t.status === c) }), {} as Record<string, Task[]>))
  );



  ngOnInit() {
    this.taskService.getTasks().pipe(take(1)).subscribe(tasks => {
      this.tasksSubject.next(tasks);
    });
  }

  public drop(event: CdkDragDrop<any[]>) {
    const previousColumn = event.previousContainer.id;
    const currentColumn = event.container.id;

    if (previousColumn === currentColumn) return;

    const tasks = this.tasksSubject.getValue();

    const task = event.previousContainer.data[event.previousIndex];

    // ✅ Update locally
    const updatedTasks = tasks.map(t =>
      t.id === task.id ? { ...t, status: currentColumn } : t
    );

    this.tasksSubject.next(updatedTasks);

    // ✅ Call API
    this.taskService.updateTaskStatus(task.id, currentColumn).subscribe({
      error: () => {
        // rollback if API fails
        this.tasksSubject.next(tasks);
      }
    });
  }

  openCreateTaskModal() {
    const dialogRef = this.dialog.open(CreateTaskComponent, {
      minHeight: '400px',
      maxHeight: '100%',
      width: '600px',
      data: { title: 'Create', users: this.users$ }
    });

    dialogRef.afterClosed()
      .pipe(take(1), filter(Boolean),
        switchMap((newTask: CreateTaskInterface) => this.taskService.createTask(newTask)),
        tap((createdTask) => {
          const updatedTasks = [
            ...this.tasksSubject.getValue(),
            createdTask
          ];
          this.tasksSubject.next(updatedTasks);
        })
      )
      .subscribe();
  }

  public openEditTask(editTask: Task) {
    const dialogRef = this.dialog.open(CreateTaskComponent, {
      minHeight: '400px',
      maxHeight: '100%',
      width: '600px',
      data: { title: 'Edit', users: this.users$, task: editTask },
    });

    dialogRef.afterClosed()
      .pipe(take(1), filter(Boolean),
        switchMap((editedTask: CreateTaskInterface) => this.taskService.editTask(editTask.id, editedTask)),
        tap((updatedTask) => {
          const tasks = this.tasksSubject.getValue();
          const updated = tasks.map(task =>
            task.id === updatedTask.id ? updatedTask : task
          );
          this.tasksSubject.next(updated);
        })
      )
      .subscribe();
  }
}

