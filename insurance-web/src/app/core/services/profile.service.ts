import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../api.config';
import { UserProfile } from '../models/profile/profile.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private http = inject(HttpClient);

  // Always fresh from the server (also confirms the account is still active).
  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${API_URL}/auth/me`);
  }
}
