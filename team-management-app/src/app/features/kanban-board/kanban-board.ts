import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { CreateTaskInterface, Task } from '../../core/services/tasks/task-interface';
import { TaskService } from '../../core/services/tasks/task.service';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, filter, finalize, map, Observable, switchMap, take, tap } from 'rxjs';
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
import { NotificationService } from '../../core/services/notification/notification';
import { TaskProjectionMessage } from '../../core/services/notification/notification.interface';
import { HttpErrorResponse } from '@angular/common/http';
import { SprintSummaryService } from '../../core/services/ai/sprint-summary.service';
import { SprintSummaryResponse } from '../../core/services/ai/sprint-summary.interface';

interface DashboardChartItem {
  label: string;
  count: number;
  percentage: number;
  color: string;
}

interface DashboardMetrics {
  total: number;
  completed: number;
  active: number;
  pending: number;
  overdue: number;
  dueSoon: number;
  completionPercentage: number;
  statuses: DashboardChartItem[];
  priorities: DashboardChartItem[];
}

@Component({
  selector: 'app-kanban-board',
  imports: [CommonModule, DragDropModule, MatButtonModule, MatDialogModule, MatIconModule, MatSnackBarModule],
  templateUrl: './kanban-board.html',
  styleUrls: ['./kanban-board.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KanbanBoard implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly sprintSummaryService = inject(SprintSummaryService);
  private readonly dialog = inject(MatDialog);
  private readonly userService = inject(UsersService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly notificationService = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  public readonly columns = Object.values(TaskStatus);
  public users$: Observable<Users[]> = this.userService.getAllUsers();
  private tasksSubject = new BehaviorSubject<Task[]>([]);
  public taskStatusLabels = TASK_STATUS_LABELS;
  public readonly isSummaryLoading = signal(false);
  public readonly sprintSummary = signal<string | null>(null);
  public readonly summaryError = signal<string | null>(null);

  private readonly dialogConfig = {
    minHeight: '400px',// ... existing code ...
    maxHeight: '60vh',
    minWidth: '60vw',
    maxWidth: 'fit-content',
  };

  public tasksByStatus$ = this.tasksSubject.pipe(
    map(tasks => {
      const grouped = Object.fromEntries(
        this.columns.map(c => [c, []])
      ) as Record<string, Task[]>;

      tasks.forEach(task => {
        console.log('status:', task.status);
        console.log('available columns:', Object.keys(grouped));

        if (!grouped[task.status]) {
          console.warn('Missing group for:', task.status);
          return;
        }

        grouped[task.status].push(task);
      });
      return grouped;
    }),
    tap(() => this.cdr.markForCheck()),
    takeUntilDestroyed(this.destroyRef)
  );

  public dashboardMetrics$ = this.tasksSubject.pipe(
    map(tasks => this.buildDashboardMetrics(tasks)),
    takeUntilDestroyed(this.destroyRef)
  );

  ngOnInit() {
    this.taskService.getTasks().pipe(take(1)).subscribe(tasks => {
      this.tasksSubject.next(tasks);
    });
    this.notificationService.start();
    this.notificationService.taskProjectionUpdates$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => this.applyTaskProjection(event));

  }

  public generateSprintSummary(): void {
    const tasks = this.tasksSubject.getValue();
    const countByStatus = (status: TaskStatus) =>
      tasks.filter(task => task.status === status).length;

    this.isSummaryLoading.set(true);
    this.sprintSummary.set(null);
    this.summaryError.set(null);

    this.sprintSummaryService.generateSummary({
      completed: countByStatus(TaskStatus.DONE),
      inProgress: countByStatus(TaskStatus.IN_PROGRESS) + countByStatus(TaskStatus.REVIEW),
      blocked: 0,
      pending: countByStatus(TaskStatus.BACKLOG) + countByStatus(TaskStatus.TODO),
    }).pipe(
      take(1),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.isSummaryLoading.set(false)),
    ).subscribe({
      next: (response: SprintSummaryResponse) => {
        if (!response.success || !response.summary.trim()) {
          this.summaryError.set(response.message || 'The AI service returned an empty summary.');
          return;
        }

        this.sprintSummary.set(response.summary);
      },
      error: (error: unknown) => this.summaryError.set(getSummaryErrorMessage(error)),
    });
  }

  private buildDashboardMetrics(tasks: Task[]): DashboardMetrics {
    const countStatus = (status: TaskStatus) =>
      tasks.filter(task => task.status === status).length;
    const completed = countStatus(TaskStatus.DONE);
    const active = countStatus(TaskStatus.IN_PROGRESS) + countStatus(TaskStatus.REVIEW);
    const pending = countStatus(TaskStatus.BACKLOG) + countStatus(TaskStatus.TODO);
    const total = tasks.length;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const weekFromToday = new Date(today);
    weekFromToday.setDate(weekFromToday.getDate() + 7);
    const dueCounts = tasks.reduce(
      (counts, task) => {
        if (task.status === TaskStatus.DONE || !task.dueDate) {
          return counts;
        }

        const dueDate = new Date(String(task.dueDate));
        if (Number.isNaN(dueDate.getTime())) {
          return counts;
        }
        dueDate.setHours(0, 0, 0, 0);

        if (dueDate < today) {
          counts.overdue++;
        } else if (dueDate <= weekFromToday) {
          counts.dueSoon++;
        }
        return counts;
      },
      { overdue: 0, dueSoon: 0 }
    );

    const toChartItem = (label: string, count: number, color: string): DashboardChartItem => ({
      label,
      count,
      percentage: total === 0 ? 0 : Math.round((count / total) * 100),
      color,
    });

    const statusColors: Record<TaskStatus, string> = {
      [TaskStatus.BACKLOG]: '#64748b',
      [TaskStatus.TODO]: '#2563eb',
      [TaskStatus.IN_PROGRESS]: '#7c3aed',
      [TaskStatus.REVIEW]: '#d97706',
      [TaskStatus.DONE]: '#059669',
    };

    const statuses = this.columns.map(status =>
      toChartItem(this.taskStatusLabels[status], countStatus(status), statusColors[status])
    );
    const priorityColors: Record<string, string> = {
      Low: '#0891b2',
      Medium: '#2563eb',
      High: '#d97706',
      Critical: '#dc2626',
      Blocker: '#7f1d1d',
    };
    const priorities = Object.entries(priorityColors).map(([priority, color]) =>
      toChartItem(
        priority,
        tasks.filter(task => task.priority.toLowerCase() === priority.toLowerCase()).length,
        color
      )
    );

    return {
      total,
      completed,
      active,
      pending,
      ...dueCounts,
      completionPercentage: total === 0 ? 0 : Math.round((completed / total) * 100),
      statuses,
      priorities,
    };
  }

  public drop(event: CdkDragDrop<Task[]>) {
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
    this.taskService.getTaskActivities(editTask.id).pipe(take(1)).subscribe(activities => {
    });
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

  private applyTaskProjection(event: TaskProjectionMessage) {
    const currentTasks = this.tasksSubject.getValue();

    if (event.isDeleted) {
      this.tasksSubject.next(currentTasks.filter(task => task.id !== event.taskId));
      return;
    }

    if (!event.task) {
      return;
    }

    const existingTaskIndex = currentTasks.findIndex(task => task.id === event.task!.id);

    if (existingTaskIndex === -1) {
      this.tasksSubject.next([event.task, ...currentTasks]);
      return;
    }

    const updatedTasks = currentTasks.map(task =>
      task.id === event.task!.id ? { ...task, ...event.task } : task
    );

    this.tasksSubject.next(updatedTasks);
  }
}

function getSummaryErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const payload: unknown = error.error;
    if (typeof payload === 'object' && payload !== null && 'message' in payload) {
      const { message } = payload;
      if (typeof message === 'string' && message.trim()) {
        return message;
      }
    }
  }

  return 'Unable to generate the sprint summary. Please try again.';
}
