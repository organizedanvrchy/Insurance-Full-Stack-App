import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../api.config';
import { Payment } from '../models/payment/payment';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private http = inject(HttpClient);

  getSchedule(enrollmentId: number): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${API_URL}/enrollments/${enrollmentId}/payments`);
  }

  pay(paymentId: number): Observable<Payment> {
    return this.http.post<Payment>(`${API_URL}/payments/${paymentId}/pay`, {});
  }
}
