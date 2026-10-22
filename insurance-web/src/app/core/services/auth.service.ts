import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_URL } from '../api.config';
import { User } from '../models/auth/user';
import { RegisterRequest } from '../models/auth/registerRequest';
import { AuthResponse } from '../models/auth/authResponse';
import { LoginRequest } from '../models/auth/loginRequest';

const TOKEN_KEY = 'ins_token';
const USER_KEY = 'ins_user';
const EXP_KEY = 'ins_exp';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private _user = signal<User | null>(this.restoreSession());
  readonly user = this._user.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);
  readonly isAdmin = computed(() => this._user()?.role === 'Admin');

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_URL}/auth/register`, req)
      .pipe(tap((res) => this.setSession(res)));
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_URL}/auth/login`, req)
      .pipe(tap((res) => this.setSession(res)));
  }

  logout(): void {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  private setSession(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    localStorage.setItem(EXP_KEY, res.expiresAtUtc);
    this._user.set(res.user);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(EXP_KEY);
    this._user.set(null);
  }

  /** Runs on page refresh so the user stays logged in until the token expires. */
  private restoreSession(): User | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      const exp = localStorage.getItem(EXP_KEY);
      if (!raw || !exp || Date.parse(exp) < Date.now()) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(EXP_KEY);
        return null;
      }
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }
}
