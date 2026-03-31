import { Component, inject, input, OnInit } from '@angular/core';
import { take } from 'rxjs/internal/operators/take';
import { TaskService } from '../../core/services/tasks/task.service';
import { map, Observable } from 'rxjs';
import { TaskActivity } from '../../core/services/tasks/task-interface';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-audit-trail',
  imports: [MatIconModule, CommonModule],
  templateUrl: './audit-trail.html',
  styleUrl: './audit-trail.css',
  standalone: true
})
export class AuditTrailComponent implements OnInit {
  public taskId = input.required<number>();
   public taskActivities$ = new Observable<TaskActivity[]>();
  private readonly taskService = inject(TaskService);

  ngOnInit(): void {
    this.taskActivities$ = this.taskService.getTaskActivities(this.taskId()).pipe(map((activities) => activities.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())));
  }  

  public getIcon(eventType: string): string {
  switch (eventType) {
    case 'TASK_CREATED': return 'add_circle';
    case 'TASK_ASSIGNED': return 'person';
    case 'TASK_STATUS_CHANGED': return 'sync';
    case 'TASK_TITLE_UPDATED': return 'edit';
    case 'TASK_DESCRIPTION_UPDATED': return 'description';
    case 'TASK_DUE_DATE_UPDATED': return 'event';
    default: return 'info';
  }
}
}
