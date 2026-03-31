import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CreateTaskInterface, Task, TaskActivity } from './task-interface';
import { Observable } from 'rxjs';
import { environment } from '../../../contants/constants';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private baseUrl = environment.apiUrl + '/tasks';
  private readonly http = inject(HttpClient);

  public getTasks(): Observable<Task[]> {
    return this.http.get<Task[]>(this.baseUrl);
  }

  public updateTaskStatus(id: number, status: string): Observable<Task> {
    return this.http.put<Task>(`${this.baseUrl}/${id}/status`, { status });
  }

  public createTask(task:CreateTaskInterface){
    return this.http.post<Task>(this.baseUrl, this.normalizeDueDate(task))
  }

  public editTask(id:number,task:CreateTaskInterface){
    return this.http.put<Task>(`${this.baseUrl}/${id}`, this.normalizeDueDate(task))
  }

   public getTaskActivities(id:number){
    return this.http.get<TaskActivity[]>(`${this.baseUrl}/${id}/activities`)
  }

  public deleteTask(id:number){
    return this.http.delete<Task>(`${this.baseUrl}/${id}`)
  }

  private normalizeDueDate(task: CreateTaskInterface): CreateTaskInterface {
    if (!task.dueDate) return task;
    
    const date = new Date(task.dueDate);
    // Convert local date to UTC to prevent timezone offset issues
    const utcDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    
    return {
      ...task,
      dueDate: utcDate
    };
  }
}
