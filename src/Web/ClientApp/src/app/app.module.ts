import { APP_ID, NgModule, inject, provideAppInitializer } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LucideAngularModule, Sun, Moon, Laptop, Plus, Settings, MoreHorizontal } from 'lucide-angular';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { CounterComponent } from './counter/counter.component';
import { WeatherComponent } from './weather/weather.component';
import { TasksComponent } from './todo/todo.component';
import { ThemeToggleComponent } from './theme-toggle/theme-toggle.component';
import { API_BASE_URL } from './web-api-client';
import { AuthorizeInterceptor } from 'src/api-authorization/authorize.interceptor';
import { LoginComponent } from 'src/api-authorization/login/login.component';
import { RegisterComponent } from 'src/api-authorization/register/register.component';
import { AuthGuard } from 'src/api-authorization/auth.guard';
import { AuthService } from 'src/api-authorization/auth.service';
import { RequisitionsListComponent } from './requisitions/requisitions-list/requisitions-list';
import { PipelineBoardComponent } from './pipeline/pipeline-board/pipeline-board';
import { CandidateDetailComponent } from './candidates/candidate-detail/candidate-detail';
import { PendingInterviewsComponent } from './interviewer/pending-interviews/pending-interviews';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { ReactiveFormsModule } from '@angular/forms';
import { RequisitionFormComponent } from './requisitions/requisition-form/requisition-form';
import { ScorecardFormComponent } from './interviewer/scorecard-form/scorecard-form';
import { ScheduleInterviewComponent } from './candidates/schedule-interview/schedule-interview';
import { JobApplicationComponent } from './public/job-application/job-application';

export function getApiBaseUrl(): string {
  const url = document.getElementsByTagName('base')[0].href;
  return url.endsWith('/') ? url.slice(0, -1) : url;
}

@NgModule({
    declarations: [
        AppComponent,
        NavMenuComponent,
        HomeComponent,
        CounterComponent,
        WeatherComponent,
        TasksComponent,
        ThemeToggleComponent,
        LoginComponent,
        RegisterComponent,
        RequisitionsListComponent,
        PipelineBoardComponent,
        CandidateDetailComponent,
        PendingInterviewsComponent,
        RequisitionFormComponent,
        ScorecardFormComponent,
        ScheduleInterviewComponent,
        JobApplicationComponent
    ],
    bootstrap: [AppComponent],
    imports: [
        BrowserModule,
        FormsModule,
        ReactiveFormsModule,
        DragDropModule,
        LucideAngularModule.pick({ Sun, Moon, Laptop, Plus, Settings, MoreHorizontal }),
        RouterModule.forRoot([
            { path: '', component: HomeComponent, pathMatch: 'full' },
            { path: 'requisitions', component: RequisitionsListComponent, canActivate: [AuthGuard] },
            { path: 'requisitions/new', component: RequisitionFormComponent, canActivate: [AuthGuard] },
            { path: 'requisitions/:id/edit', component: RequisitionFormComponent, canActivate: [AuthGuard] },
            { path: 'pipeline/:id', component: PipelineBoardComponent, canActivate: [AuthGuard] },
            { path: 'candidates/:id', component: CandidateDetailComponent, canActivate: [AuthGuard] },
            { path: 'pipeline/schedule/:candidateId', component: ScheduleInterviewComponent, canActivate: [AuthGuard] },
            { path: 'interviewer', component: PendingInterviewsComponent, canActivate: [AuthGuard] },
            { path: 'interviewer/scorecard/:interviewId', component: ScorecardFormComponent, canActivate: [AuthGuard] },
            { path: 'careers/:requisitionId/apply', component: JobApplicationComponent },
            { path: 'counter', component: CounterComponent },
            { path: 'weather', component: WeatherComponent, canActivate: [AuthGuard] },
            { path: 'todo', component: TasksComponent, canActivate: [AuthGuard] },
            { path: 'login', component: LoginComponent },
            { path: 'register', component: RegisterComponent }
        ])
    ],
    providers: [
        { provide: APP_ID, useValue: 'ng-cli-universal' },
        { provide: HTTP_INTERCEPTORS, useClass: AuthorizeInterceptor, multi: true },
        { provide: API_BASE_URL, useFactory: getApiBaseUrl, deps: [] },
        provideAppInitializer(() => inject(AuthService).initialize()),
        provideHttpClient(withInterceptorsFromDi())
    ]
})
export class AppModule { }
