import { Injectable, signal, computed } from '@angular/core';

export interface CurrentUser {
  id: string;
  email: string;
  roles: string[];
  permissions: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthState {
  private readonly REFRESH_TOKEN_KEY = 'hyre_refresh_token';

  readonly accessToken = signal<string | null>(null);
  readonly user = signal<CurrentUser | null>(null);
  readonly isAuthenticated = computed(() => this.accessToken() !== null);

  setTokens(accessToken: string, refreshToken: string): void {
    this.accessToken.set(accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);
  }

  setUser(user: CurrentUser): void {
    this.user.set(user);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  clear(): void {
    this.accessToken.set(null);
    this.user.set(null);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }
}
