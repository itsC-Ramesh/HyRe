import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { Inject } from '@angular/core';
import { API_BASE_URL } from '../../web-api-client';

interface CandidateProfile {
  id: string;
  name: string;
  phone: string;
  source: string;
  sourceDetail: string;
  tags: string[];
}

interface FeedEvent {
  id: string;
  eventType: string;
  description: string;
  occurredAt: Date;
}

@Component({
  selector: 'app-candidate-detail',
  templateUrl: './candidate-detail.html',
  styleUrls: ['./candidate-detail.css']
})
export class CandidateDetailComponent implements OnInit {
  candidateId: string = '';
  candidate: CandidateProfile | null = null;
  feedEvents: FeedEvent[] = [];
  isLoading = true;

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.candidateId = params.get('id') || '';
      if (this.candidateId) {
        this.loadCandidateData();
      }
    });
  }

  loadCandidateData(): void {
    this.isLoading = true;
    
    // Simulate parallel fetching for profile and feed
    setTimeout(() => {
      this.loadMockData();
      this.isLoading = false;
    }, 500);

    // In a real app we'd use forkJoin
    /*
    forkJoin({
      profile: this.http.get<any>(`${this.baseUrl}/api/v1/candidates/${this.candidateId}`),
      feed: this.http.get<any>(`${this.baseUrl}/api/v1/communications/feed/${this.candidateId}`)
    }).subscribe({ ... });
    */
  }

  loadMockData(): void {
    this.candidate = {
      id: this.candidateId,
      name: 'Eleanor Shellstrop',
      phone: '+1 (555) 019-2834',
      source: 'LinkedIn',
      sourceDetail: 'Ad Campaign',
      tags: ['Senior', 'Angular', '.NET']
    };

    this.feedEvents = [
      { id: '1', eventType: 'Application Submitted', description: 'Applied for Senior Software Engineer', occurredAt: new Date(Date.now() - 86400000 * 2) },
      { id: '2', eventType: 'Email Sent', description: 'Screening Invitation', occurredAt: new Date(Date.now() - 86400000) },
      { id: '3', eventType: 'Note Added', description: 'Good communication skills.', occurredAt: new Date(Date.now() - 3600000) }
    ];
  }

  getIconForEvent(eventType: string): string {
    if (eventType.includes('Email')) return 'mail';
    if (eventType.includes('Note')) return 'file-text';
    if (eventType.includes('Interview')) return 'calendar';
    return 'activity';
  }
}
