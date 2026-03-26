import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DeleteTask } from './delete-task';

describe('DeleteTask', () => {
  let component: DeleteTask;
  let fixture: ComponentFixture<DeleteTask>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DeleteTask],
    }).compileComponents();

    fixture = TestBed.createComponent(DeleteTask);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
