import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { CreateTaskInterface, Task } from './task-interface';
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
    return this.http.post<Task>(this.baseUrl,task)
  }

  public editTask(id:number,task:CreateTaskInterface){
    return this.http.put<Task>(`${this.baseUrl}/${id}`,task)
  }
}
