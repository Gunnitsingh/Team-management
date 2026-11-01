import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SprintSummaryService } from './sprint-summary.service';
import { SprintSummaryRequest, SprintSummaryResponse } from './sprint-summary.interface';

describe('SprintSummaryService', () => {
  let service: SprintSummaryService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SprintSummaryService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('posts the sprint metrics to the AI summary endpoint', () => {
    const request: SprintSummaryRequest = {
      completed: 3,
      inProgress: 2,
      blocked: 0,
      pending: 4,
    };
    const response: SprintSummaryResponse = {
      success: true,
      summary: 'Sprint is progressing well.',
      message: null,
      promptTokens: 50,
      responseTokens: 20,
    };

    service.generateSummary(request).subscribe(result => {
      expect(result).toEqual(response);
    });

    const httpRequest = httpTestingController.expectOne(
      'http://localhost:8080/api/ai/sprint-summary',
    );
    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);
    httpRequest.flush(response);
  });
});
