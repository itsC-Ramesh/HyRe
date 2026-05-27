import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthState, CurrentUser } from './auth.state';
import { environment } from '../../../environments/environment';

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: { code: string; message: string; details?: unknown };
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private state = inject(AuthState);
  private router = inject(Router);

  get isAuthenticated() {
    return this.state.isAuthenticated;
  }

  get user() {
    return this.state.user;
  }

  get accessToken() {
    return this.state.accessToken;
  }

  hasPermission(permission: string): boolean {
    const user = this.state.user();
    return user?.permissions.includes(permission) ?? false;
  }

  async login(email: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<LoginResponse>>(`${environment.apiUrl}/Auth/login`, {
        email,
        password,
      })
    );

    if (!response.success || !response.data) {
      throw new Error(response.error?.message ?? 'Login failed');
    }

    this.state.setTokens(response.data.accessToken, response.data.refreshToken);
    await this.fetchCurrentUser();
  }

  async fetchCurrentUser(): Promise<void> {
    const response = await firstValueFrom(
      this.http.get<ApiResponse<CurrentUser>>(`${environment.apiUrl}/Auth/me`)
    );

    if (!response.success || !response.data) {
      throw new Error('Failed to fetch user');
    }

    this.state.setUser(response.data);
  }

  async refreshToken(): Promise<boolean> {
    const refreshToken = this.state.getRefreshToken();
    const accessToken = this.state.accessToken();

    if (!refreshToken || !accessToken) {
      return false;
    }

    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<LoginResponse>>(`${environment.apiUrl}/Auth/refresh`, {
          accessToken,
          refreshToken,
        })
      );

      if (!response.success || !response.data) {
        return false;
      }

      this.state.setTokens(response.data.accessToken, response.data.refreshToken);
      return true;
    } catch {
      return false;
    }
  }

  async logout(): Promise<void> {
    const refreshToken = this.state.getRefreshToken();

    if (refreshToken) {
      try {
        await firstValueFrom(
          this.http.post(`${environment.apiUrl}/Auth/logout`, { refreshToken })
        );
      } catch {
        // Logout API failure is non-critical
      }
    }

    this.state.clear();
    this.router.navigate(['/login']);
  }
}
