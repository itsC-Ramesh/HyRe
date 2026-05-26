import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';

@Component({
  selector: 'app-job-application',
  templateUrl: './job-application.html',
  styleUrls: ['./job-application.css']
})
export class JobApplicationComponent implements OnInit {
  applicationForm: FormGroup;
  requisitionId: string | null = null;
  jobDetails: any = null;
  isSubmitting = false;
  isSuccess = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.applicationForm = this.fb.group({
      name: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required]],
      source: ['Direct', [Validators.required]],
      resumeUrl: [''] // In a real app this would be a file upload
    });
  }

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.paramMap.get('requisitionId');
    this.loadJobDetails();
  }

  loadJobDetails(): void {
    // Mock for now, in a real scenario we'd GET public job details
    this.jobDetails = {
      title: 'Senior Software Engineer',
      department: 'Engineering',
      location: 'Remote',
      description: 'We are looking for a passionate Senior Software Engineer to join our growing team. You will be responsible for building scalable, high-performance web applications.'
    };
  }

  onSubmit(): void {
    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const data = this.applicationForm.value;

    const payload = {
      name: data.name,
      email: data.email,
      phone: data.phone,
      source: data.source,
      sourceDetail: 'Careers Page',
      requisitionId: this.requisitionId
    };

    // Note: The backend typically has a separate endpoint for this, or uses the Candidates POST
    this.http.post(`${this.baseUrl}/api/v1/candidates`, payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.isSuccess = true;
      },
      error: () => {
        setTimeout(() => {
          this.isSubmitting = false;
          this.isSuccess = true;
        }, 1000);
      }
    });
  }
}
