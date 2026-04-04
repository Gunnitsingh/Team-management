import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AuditGroup, CreateTaskInterface, Task, TaskActivity } from './task-interface';
import { Observable } from 'rxjs';
import { environment } from '../../../contants/constants';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private baseUrl = environment.apiUrl + '/tasks';
  private readonly http = inject(HttpClient);
  private readonly headers = {
  'X-User-Id': '1',
  'X-User-Name': 'Guneet Singh'
};

  public getTasks(): Observable<Task[]> {
    return this.http.get<Task[]>( this.baseUrl);
  }

  public updateTaskStatus(id: number, status: string): Observable<Task> {
    return this.http.put<Task>(`${this.baseUrl}/${id}/status`, { status }, {headers: this.headers});
  }

  public createTask(task:CreateTaskInterface){
    return this.http.post<Task>(this.baseUrl, this.normalizeDueDate(task), {headers: this.headers})
  }

  public editTask(id:number,task:CreateTaskInterface){
    return this.http.put<Task>(`${this.baseUrl}/${id}`, this.normalizeDueDate(task), {headers: this.headers})
  }

   public getTaskActivities(id:number){
    return this.http.get<AuditGroup[]>(`${this.baseUrl}/${id}/activities`)
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
