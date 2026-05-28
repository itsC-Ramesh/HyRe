import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { InternalLayout } from './core/layout/internal-layout/internal-layout';
import { CandidateLayout } from './core/layout/candidate-layout/candidate-layout';

export const routes: Routes = [
  // Public
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },

  // Internal app
  {
    path: '',
    canActivate: [authGuard],
    component: InternalLayout,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        canActivate: [permissionGuard('requisitions:read')],
        loadComponent: () =>
          import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      // Requisitions
      {
        path: 'requisitions',
        canActivate: [permissionGuard('requisitions:read')],
        loadComponent: () =>
          import('./features/requisitions/requisition-list').then((m) => m.RequisitionList),
      },
      {
        path: 'requisitions/new',
        canActivate: [permissionGuard('requisitions:create')],
        loadComponent: () =>
          import('./features/requisitions/requisition-form').then((m) => m.RequisitionForm),
      },
      {
        path: 'requisitions/:id',
        canActivate: [permissionGuard('requisitions:read')],
        loadComponent: () =>
          import('./features/requisitions/requisition-detail').then((m) => m.RequisitionDetail),
      },
      {
        path: 'requisitions/:id/edit',
        canActivate: [permissionGuard('requisitions:update')],
        loadComponent: () =>
          import('./features/requisitions/requisition-form').then((m) => m.RequisitionForm),
      },
      // Candidates
      {
        path: 'candidates',
        canActivate: [permissionGuard('candidates:read')],
        loadComponent: () =>
          import('./features/candidates/candidate-list').then((m) => m.CandidateList),
      },
      {
        path: 'candidates/new',
        canActivate: [permissionGuard('candidates:create')],
        loadComponent: () =>
          import('./features/candidates/candidate-form').then((m) => m.CandidateForm),
      },
      {
        path: 'candidates/:id',
        canActivate: [permissionGuard('candidates:read')],
        loadComponent: () =>
          import('./features/candidates/candidate-detail').then((m) => m.CandidateDetail),
      },
      {
        path: 'candidates/:id/edit',
        canActivate: [permissionGuard('candidates:update')],
        loadComponent: () =>
          import('./features/candidates/candidate-form').then((m) => m.CandidateForm),
      },
      // Pipeline
      {
        path: 'pipeline/:requisitionId',
        canActivate: [permissionGuard('pipeline:read')],
        loadComponent: () =>
          import('./features/pipeline/pipeline-board').then((m) => m.PipelineBoard),
      },
    ],
  },

  // Candidate portal
  {
    path: 'portal',
    canActivate: [authGuard],
    component: CandidateLayout,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
