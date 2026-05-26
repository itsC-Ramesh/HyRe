import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PipelineBoard } from './pipeline-board';

describe('PipelineBoard', () => {
  let component: PipelineBoard;
  let fixture: ComponentFixture<PipelineBoard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PipelineBoard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PipelineBoard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
