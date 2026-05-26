import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import { AuthClient, LoginCommand, RegisterCandidateCommand, LogoutCommand } from '../app/web-api-client';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private _isAuthenticated = new BehaviorSubject<boolean>(false);
  isAuthenticated$ = this._isAuthenticated.asObservable();

  constructor(private authClient: AuthClient) {}

  initialize(): Observable<boolean> {
    // Assume not authenticated on app start — will be set on login
    return of(false);
  }

  login(email: string, password: string): Observable<void> {
    return this.authClient.login(new LoginCommand({ email, password })).pipe(
      tap(() => this._isAuthenticated.next(true)),
    );
  }

  register(email: string, password: string): Observable<void> {
    return this.authClient.register(new RegisterCandidateCommand({ email, password }));
  }

  logout(): Observable<void> {
    return this.authClient.logout(new LogoutCommand({ refreshToken: '' })).pipe(
      tap(() => this._isAuthenticated.next(false))
    );
  }
}
