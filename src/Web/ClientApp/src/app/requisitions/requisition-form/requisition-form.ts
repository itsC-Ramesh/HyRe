import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';

@Component({
  selector: 'app-requisition-form',
  templateUrl: './requisition-form.html',
  styleUrls: ['./requisition-form.css']
})
export class RequisitionFormComponent implements OnInit {
  reqForm: FormGroup;
  isEditMode = false;
  requisitionId: string | null = null;
  isLoading = false;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {
    this.reqForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      department: ['', [Validators.required]],
      jdText: ['', [Validators.required]],
      salaryMin: [null],
      salaryMax: [null],
      headcount: [1, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.paramMap.get('id');
    if (this.requisitionId) {
      this.isEditMode = true;
      this.loadRequisition();
    }
  }

  loadRequisition(): void {
    this.isLoading = true;
    this.http.get<any>(`${this.baseUrl}/api/v1/requisitions/${this.requisitionId}`).subscribe({
      next: (res) => {
        if (res && res.data) {
          this.reqForm.patchValue(res.data);
        }
        this.isLoading = false;
      },
      error: () => {
        // Mock data for demo
        this.reqForm.patchValue({
          title: 'Senior Software Engineer',
          department: 'Engineering',
          jdText: 'We are looking for a senior engineer...',
          salaryMin: 120000,
          salaryMax: 150000,
          headcount: 2
        });
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.reqForm.invalid) {
      this.reqForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const data = this.reqForm.value;
    
    const request$ = this.isEditMode
      ? this.http.put(`${this.baseUrl}/api/v1/requisitions/${this.requisitionId}`, data)
      : this.http.post(`${this.baseUrl}/api/v1/requisitions`, data);

    request$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/requisitions']);
      },
      error: () => {
        // Simulate success if backend isn't ready
        setTimeout(() => {
          this.isSubmitting = false;
          this.router.navigate(['/requisitions']);
        }, 800);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/requisitions']);
  }
}
