import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';

@Component({
  selector: 'app-schedule-interview',
  templateUrl: './schedule-interview.html',
  styleUrls: ['./schedule-interview.css']
})
export class ScheduleInterviewComponent implements OnInit {
  scheduleForm: FormGroup;
  candidateId: string | null = null;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.scheduleForm = this.fb.group({
      interviewerId: ['', [Validators.required]],
      type: [1, [Validators.required]], // Default to Phone/Video
      scheduledAt: ['', [Validators.required]],
      durationMin: [45, [Validators.required, Validators.min(15)]],
      meetingLink: ['']
    });
  }

  ngOnInit(): void {
    this.candidateId = this.route.snapshot.paramMap.get('candidateId');
  }

  onSubmit(): void {
    if (this.scheduleForm.invalid) {
      this.scheduleForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const data = this.scheduleForm.value;
    
    // Convert local datetime-local string to proper ISO date
    const date = new Date(data.scheduledAt);
    
    const payload = {
      applicationId: this.candidateId, // Simple mapping for now
      interviewerId: data.interviewerId,
      type: Number(data.type),
      scheduledAt: date.toISOString(),
      durationMin: Number(data.durationMin),
      meetingLink: data.meetingLink
    };

    this.http.post(`${this.baseUrl}/api/v1/interviews/schedule`, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/pipeline', 'current']);
      },
      error: () => {
        setTimeout(() => {
          this.isSubmitting = false;
          // Go back to candidate or pipeline
          this.router.navigate(['/candidates', this.candidateId]);
        }, 800);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/candidates', this.candidateId]);
  }
}
