import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';

interface Interview {
  id: string;
  candidateName: string;
  requisitionTitle: string;
  scheduledAt: Date;
  status: number;
}

@Component({
  selector: 'app-pending-interviews',
  templateUrl: './pending-interviews.html',
  styleUrls: ['./pending-interviews.css']
})
export class PendingInterviewsComponent implements OnInit {
  interviews: Interview[] = [];
  isLoading = true;

  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  ngOnInit(): void {
    this.loadInterviews();
  }

  loadInterviews(): void {
    this.isLoading = true;
    
    setTimeout(() => {
      this.loadMockData();
      this.isLoading = false;
    }, 500);
    
    // this.http.get<any>(`${this.baseUrl}/api/v1/interviews/pending`).subscribe(...)
  }

  loadMockData(): void {
    this.interviews = [
      { id: '1', candidateName: 'Eleanor Shellstrop', requisitionTitle: 'Senior Software Engineer', scheduledAt: new Date(Date.now() + 86400000), status: 0 },
      { id: '2', candidateName: 'Chidi Anagonye', requisitionTitle: 'Ethics Consultant', scheduledAt: new Date(Date.now() + 172800000), status: 0 }
    ];
  }

  openScorecard(interviewId: string): void {
    console.log('Open scorecard for', interviewId);
    // Navigate to scorecard or open modal
  }
}
