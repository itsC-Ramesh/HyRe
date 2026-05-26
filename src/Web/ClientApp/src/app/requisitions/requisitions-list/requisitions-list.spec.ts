import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequisitionsList } from './requisitions-list';

describe('RequisitionsList', () => {
  let component: RequisitionsList;
  let fixture: ComponentFixture<RequisitionsList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequisitionsList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RequisitionsList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
