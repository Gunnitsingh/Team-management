import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../contants/constants';
import { SprintSummaryRequest, SprintSummaryResponse } from './sprint-summary.interface';

@Injectable({
  providedIn: 'root',
})
export class SprintSummaryService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = `${environment.apiUrl}/ai/sprint-summary`;

  public generateSummary(request: SprintSummaryRequest): Observable<SprintSummaryResponse> {
    return this.http.post<SprintSummaryResponse>(this.endpoint, request);
  }
}
