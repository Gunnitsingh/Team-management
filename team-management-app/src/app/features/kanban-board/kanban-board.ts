import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CreateTaskInterface, Task } from '../../core/services/tasks/task-interface';
import { TaskService } from '../../core/services/tasks/task.service';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, filter, map, Observable, switchMap, take, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { TASK_STATUS_LABELS, TaskStatus } from '../../contants/constants';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CreateTaskComponent } from '../create-task/create-task';
import { Users } from '../../core/services/users/users.interface';
import { UsersService } from '../../core/services/users/users';
import { MatIconModule } from '@angular/material/icon';
import { DeleteTask } from '../delete-task/delete-task';

@Component({
  selector: 'app-kanban-board',
  imports: [CommonModule, DragDropModule, MatButtonModule, MatDialogModule, MatIconModule, MatSnackBarModule],
  templateUrl: './kanban-board.html',
  styleUrls: ['./kanban-board.css'],
  standalone: true
})
export class KanbanBoard implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  private readonly userService = inject(UsersService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  
  public readonly columns = Object.values(TaskStatus);
  public users$: Observable<Users[]> = this.userService.getAllUsers();
  private tasksSubject = new BehaviorSubject<Task[]>([]);
  public taskStatusLabels = TASK_STATUS_LABELS;
  
  private readonly dialogConfig = {
    minHeight: '400px',
    maxHeight: '100%',
    width: '600px'
  };

  public tasksByStatus$: Observable<Record<string, Task[]>> = this.tasksSubject.pipe(
    map(tasks => this.columns.reduce((acc, c) => ({ ...acc, [c]: tasks.filter(t => t.status === c) }), {} as Record<string, Task[]>)),
    takeUntilDestroyed(this.destroyRef)
  );



  ngOnInit() {
    this.taskService.getTasks().pipe(take(1)).subscribe(tasks => {
      this.tasksSubject.next(tasks);
    });
  }

  public drop(event: CdkDragDrop<any[]>) {
    const previousColumn = event.previousContainer.id;
    const currentColumn = event.container.id as TaskStatus;

    if (previousColumn === currentColumn) return;

    const tasks = this.tasksSubject.getValue();
    const task = event.previousContainer.data[event.previousIndex];

    // ✅ Update locally
    const updatedTasks = tasks.map(t =>
      t.id === task.id ? { ...t, status: currentColumn } : t
    );

    this.tasksSubject.next(updatedTasks);

    // ✅ Call API
    this.taskService.updateTaskStatus(task.id, currentColumn)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          // rollback if API fails
          this.tasksSubject.next(tasks);
          this.snackBar.open('Failed to update task status', 'Close', { duration: 3000 });
        }
      });
  }

  openCreateTaskModal() {
    const dialogRef = this.dialog.open(CreateTaskComponent, {
      ...this.dialogConfig,
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
          this.snackBar.open('Task created successfully', 'Close', { duration: 2000 });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        error: (err) => {
          console.error('Failed to create task', err);
          this.snackBar.open('Failed to create task', 'Close', { duration: 3000 });
        }
      });
  }

  public openEditTask(editTask: Task) {
    const dialogRef = this.dialog.open(CreateTaskComponent, {
      ...this.dialogConfig,
      data: { title: 'Update', users: this.users$, task: editTask },
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
          this.snackBar.open('Task updated successfully', 'Close', { duration: 2000 });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        error: (err) => {
          console.error('Failed to update task', err);
          this.snackBar.open('Failed to update task', 'Close', { duration: 3000 });
        }
      });
  }

  public deleteTask(task: Task) {
    const dialogRef = this.dialog.open(DeleteTask, {
      data: { task },
    });

    dialogRef.afterClosed()
      .pipe(take(1), filter(Boolean),
        switchMap(() => this.taskService.deleteTask(task.id)),
        tap(() => {
          const updated = this.tasksSubject
            .getValue()
            .filter(t => t.id !== task.id);

          this.tasksSubject.next(updated);
          this.snackBar.open('Task deleted successfully', 'Close', { duration: 2000 });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        error: (err) => {
          console.error('Failed to delete task', err);
          this.snackBar.open('Failed to delete task', 'Close', { duration: 3000 });
        }
      });
  }

  public trackByTask(index: number, task: Task): number {
    return task.id;
  }
}

