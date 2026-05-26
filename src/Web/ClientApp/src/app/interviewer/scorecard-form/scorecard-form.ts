import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';

@Component({
  selector: 'app-scorecard-form',
  templateUrl: './scorecard-form.html',
  styleUrls: ['./scorecard-form.css']
})
export class ScorecardFormComponent implements OnInit {
  scorecardForm: FormGroup;
  interviewId: string | null = null;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.scorecardForm = this.fb.group({
      technical: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
      communication: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
      culture: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
      strengths: ['', [Validators.required]],
      concerns: ['', [Validators.required]],
      recommendation: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.interviewId = this.route.snapshot.paramMap.get('interviewId');
  }

  onSubmit(): void {
    if (this.scorecardForm.invalid) {
      this.scorecardForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const data = this.scorecardForm.value;
    
    // Transform flat ratings to JSON structure expected by backend
    const payload = {
      ratings: {
        technical: data.technical,
        communication: data.communication,
        culture: data.culture
      },
      recommendation: data.recommendation,
      strengths: data.strengths,
      concerns: data.concerns,
      notes: ''
    };

    this.http.post(`${this.baseUrl}/api/v1/scorecards/${this.interviewId}`, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/interviewer']);
      },
      error: () => {
        setTimeout(() => {
          this.isSubmitting = false;
          this.router.navigate(['/interviewer']);
        }, 800);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/interviewer']);
  }
}
