import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuditTrailComponent } from './audit-trail';

describe('AuditTrail', () => {
  let component: AuditTrailComponent;
  let fixture: ComponentFixture<AuditTrailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditTrailComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(AuditTrailComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
