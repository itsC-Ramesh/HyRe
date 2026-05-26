import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';
import { Router } from '@angular/router';

interface Requisition {
  id: string;
  title: string;
  department: string;
  status: number;
  headcount: number;
  salaryMin?: number;
  salaryMax?: number;
}

@Component({
  selector: 'app-requisitions-list',
  templateUrl: './requisitions-list.html',
  styleUrls: ['./requisitions-list.css']
})
export class RequisitionsListComponent implements OnInit {
  requisitions: Requisition[] = [];
  isLoading = true;

  constructor(
    private http: HttpClient,
    private router: Router,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  ngOnInit(): void {
    this.loadRequisitions();
  }

  loadRequisitions(): void {
    this.isLoading = true;
    this.http.get<any>(`${this.baseUrl}/api/v1/requisitions`)
      .subscribe({
        next: (res) => {
          if (res && res.data && res.data.items) {
             this.requisitions = res.data.items;
          } else {
             this.loadMockData();
          }
          this.isLoading = false;
        },
        error: () => {
          this.loadMockData();
          this.isLoading = false;
        }
      });
  }

  loadMockData(): void {
    this.requisitions = [
      { id: 'req1', title: 'Senior Software Engineer', department: 'Engineering', status: 1, headcount: 2, salaryMin: 120000, salaryMax: 150000 },
      { id: 'req2', title: 'Product Manager', department: 'Product', status: 1, headcount: 1, salaryMin: 110000, salaryMax: 140000 },
      { id: 'req3', title: 'UX Designer', department: 'Design', status: 0, headcount: 1, salaryMin: 90000, salaryMax: 120000 }
    ];
  }

  openPipeline(id: string): void {
    this.router.navigate(['/pipeline', id]);
  }

  getStatusBadgeClass(status: number): string {
    switch (status) {
      case 0: return 'badge-draft';
      case 1: return 'badge-open';
      case 2: return 'badge-closed';
      case 3: return 'badge-on-hold';
      default: return 'badge-default';
    }
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'Draft';
      case 1: return 'Open';
      case 2: return 'Closed';
      case 3: return 'On Hold';
      default: return 'Unknown';
    }
  }
}
