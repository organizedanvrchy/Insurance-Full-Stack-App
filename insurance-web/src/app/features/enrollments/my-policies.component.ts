import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { getErrorMessage } from '../../core/utils/http-error';
import { Enrollment } from '../../core/models/enrollment/enrollment';

@Component({
  selector: 'app-my-policies',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: "my-policies.html",
})
export class MyPoliciesComponent {
  private service = inject(EnrollmentService);

  enrollments = signal<Enrollment[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  // How many policies need attention (the API decides; we just count).
  overdueCount = computed(() => this.enrollments().filter((e) => e.paymentAlert === 'Overdue').length);
  dueSoonCount = computed(() => this.enrollments().filter((e) => e.paymentAlert === 'DueSoon').length);

  constructor() {
    this.load();
  }

  cancel(e: Enrollment): void {
    if (!confirm(`Cancel "${e.policyName}"? Unpaid premiums will be removed.`)) return;
    this.service.cancel(e.enrollmentId).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(getErrorMessage(err)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.getMine().subscribe({
      next: (list) => {
        this.enrollments.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(getErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
