import { Task } from '../tasks/task-interface';

export interface SignalRNotification {
    id: number;
    title: string;
    message: string;
    userId: string;
    createdAt: string;
}

export interface TaskProjectionMessage {
    eventType: string;
    taskId: number;
    isDeleted: boolean;
    task?: Task;
}
