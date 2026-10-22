import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../api.config';
import { ClaimStatus } from '../models/enums';
import { Claim } from '../models/claim/claim';
import { ClaimReviewRequest } from '../models/claim/claimReviewRequest';
import { ClaimSubmitRequest } from '../models/claim/claimSubmitRequest';

@Injectable({ providedIn: 'root' })
export class ClaimService {
  private http = inject(HttpClient);
  private url = `${API_URL}/claims`;

  submit(req: ClaimSubmitRequest): Observable<Claim> {
    return this.http.post<Claim>(this.url, req);
  }

  getMine(): Observable<Claim[]> {
    return this.http.get<Claim[]>(`${this.url}/mine`);
  }

  // Admin only. Omit `status` for all claims.
  getAll(status?: ClaimStatus): Observable<Claim[]> {
    return this.http.get<Claim[]>(this.url, { params: status ? { status } : {} });
  }

  review(id: number, req: ClaimReviewRequest): Observable<Claim> {
    return this.http.put<Claim>(`${this.url}/${id}/review`, req);
  }
}
