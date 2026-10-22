import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../api.config';
import { Question } from '../models/quote/Question';
import { QuoteRequest } from '../models/quote/QuoteRequest';
import { Quote } from '../models/quote/Quote';

@Injectable({ providedIn: 'root' })
export class QuoteService {
  private http = inject(HttpClient);

  getQuestions(policyId: number): Observable<Question[]> {
    return this.http.get<Question[]>(`${API_URL}/quotes/questions/${policyId}`);
  }

  create(req: QuoteRequest): Observable<Quote> {
    return this.http.post<Quote>(`${API_URL}/quotes`, req);
  }
}
