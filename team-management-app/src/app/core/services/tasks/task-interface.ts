export interface Task {
  id: number;
  title: string;
  status: string;
  priority: string;
  description: string;
  assignedToId : number | null;
  assignedToName?: string;
  dueDate : Date
}

export interface CreateTaskInterface {
  id:string,
  assignedTo: number,
  description: string,
  priority: string,
  title: string
  dueDate : Date
}

export interface  TaskActivity {
  id: number;
  taskId: number;
  description: string;
  eventType: string;
  newValue: string;
  createdAt: Date;
}

export interface AuditGroup {
  correlationId: string;
  changedByName: string;
  timestamp: string;
  changes: AuditChange[];
}

interface AuditChange {
  description: string;
  eventType: string;
}