import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { KanbanBoard } from './features/kanban-board/kanban-board';

@Component({
  selector: 'app-root',
  imports: [ KanbanBoard],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('team-management-app');
}
