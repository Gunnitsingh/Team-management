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