export const kanbanColumns = ['BACKLOG', 'TODO', 'IN PROGRESS', 'REVIEW', 'DONE'];

export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080/api',
};

export enum TaskStatus {
  BACKLOG = 'BACKLOG',
  TODO = 'TODO',
  IN_PROGRESS = 'IN_PROGRESS',
  REVIEW = 'REVIEW',
  DONE = 'DONE'
}

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  [TaskStatus.BACKLOG]: 'Backlog',
  [TaskStatus.TODO]: 'To Do',
  [TaskStatus.IN_PROGRESS]: 'In Progress',
  [TaskStatus.REVIEW]: 'Review',
  [TaskStatus.DONE]: 'Done'
};
