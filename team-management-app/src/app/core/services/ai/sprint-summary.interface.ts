export interface SprintSummaryRequest {
  completed: number;
  inProgress: number;
  blocked: number;
  pending: number;
}

export interface SprintSummaryResponse {
  success: boolean;
  summary: string;
  message: string | null;
  promptTokens: number;
  responseTokens: number;
}
