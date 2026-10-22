import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../api.config';
import { Policy } from '../models/policy/policy';
import { PolicyRequest } from '../models/policy/policyRequest';

// One service per API controller. Components never build URLs themselves.
@Injectable({ providedIn: 'root' })
export class PolicyService {
  private http = inject(HttpClient);
  private url = `${API_URL}/policies`;

  getAll(includeInactive = false): Observable<Policy[]> {
    return this.http.get<Policy[]>(this.url, { params: { includeInactive } });
  }

  getById(id: number): Observable<Policy> {
    return this.http.get<Policy>(`${this.url}/${id}`);
  }

  create(req: PolicyRequest): Observable<Policy> {
    return this.http.post<Policy>(this.url, req);        // 201 + the created policy
  }

  update(id: number, req: PolicyRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, req); // 204 No Content
  }

  deactivate(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);   // 204 No Content
  }
}
