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
