import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { API_URL } from "../api.config";
import { EnrollmentRequest } from "../models/enrollment/enrollmentRequest";
import { Observable } from "rxjs";
import { Enrollment } from "../models/enrollment/enrollment";



@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private http = inject(HttpClient);
  private url = `${API_URL}/enrollments`;

  enroll(req: EnrollmentRequest): Observable<Enrollment> {
    return this.http.post<Enrollment>(this.url, req);
  }

  getMine(): Observable<Enrollment[]> {
    return this.http.get<Enrollment[]>(`${this.url}/mine`);
  }

  getById(id: number): Observable<Enrollment> {
    return this.http.get<Enrollment>(`${this.url}/${id}`);
  }

  cancel(id: number): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/cancel`, {});
  }
}
