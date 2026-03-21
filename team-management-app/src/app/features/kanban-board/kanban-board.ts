import { Component, inject, OnInit } from '@angular/core';
import { Task } from '../../core/services/task-interface';
import { TaskService } from '../../core/services/task.service';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, map, Observable, take } from 'rxjs';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { kanbanColumns } from '../../contants/constants';

@Component({
  selector: 'app-kanban-board',
  imports: [CommonModule, DragDropModule],
  templateUrl: './kanban-board.html',
  styleUrl: './kanban-board.css',
})
export class KanbanBoard implements OnInit {

  public columns = kanbanColumns;
  tasksByStatus$ = new Observable<any>();
  private tasksSubject = new BehaviorSubject<Task[]>([]);
  private readonly taskService = inject(TaskService);
  ngOnInit() {
    this.taskService.getTasks().pipe(take(1)).subscribe(tasks => {
      this.tasksSubject.next(tasks);
    });
    this.getTasks();
  }

  getTasks() {
    this.tasksByStatus$ = this.tasksSubject.asObservable().pipe(
      map(tasks => {
        const grouped: any = {};
        this.columns.forEach(col => {
          grouped[col] = tasks.filter(t => t.status === col);
        });
        return grouped;
      })
    );
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
}
