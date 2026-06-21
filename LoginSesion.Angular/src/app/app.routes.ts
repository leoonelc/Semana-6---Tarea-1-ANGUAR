import { Routes } from '@angular/router';

import { authGuard } from './core/auth.guard';
import { DashboardPage } from './pages/dashboard/dashboard';
import { LoginPage } from './pages/login/login';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  { path: 'dashboard', component: DashboardPage, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' }
];
