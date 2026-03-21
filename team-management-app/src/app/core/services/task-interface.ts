export interface Task {
  id: number;
  title: string;
  status: string;
  priority: string;
  description:string;
  assignedToName?: string;
}