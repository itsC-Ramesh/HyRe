import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PendingInterviews } from './pending-interviews';

describe('PendingInterviews', () => {
  let component: PendingInterviews;
  let fixture: ComponentFixture<PendingInterviews>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PendingInterviews]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PendingInterviews);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
