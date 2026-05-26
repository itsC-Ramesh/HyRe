import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScorecardForm } from './scorecard-form';

describe('ScorecardForm', () => {
  let component: ScorecardForm;
  let fixture: ComponentFixture<ScorecardForm>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScorecardForm]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ScorecardForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
