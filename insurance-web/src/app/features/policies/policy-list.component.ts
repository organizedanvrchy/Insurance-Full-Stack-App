import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { POLICY_TYPES } from '../../core/models/enums';
import { AuthService } from '../../core/services/auth.service';
import { PolicyService } from '../../core/services/policy.service';
import { getErrorMessage } from '../../core/utils/http-error';
import { Policy } from '../../core/models/policy/policy';

@Component({
  selector: 'app-policy-list',
  imports: [RouterLink, CurrencyPipe],
  templateUrl: "./policy-list.html",
  styleUrl: "./policy-list.component.css",
})
export class PolicyListComponent {
  auth = inject(AuthService);
  private policies = inject(PolicyService);

  readonly policyTypes = POLICY_TYPES;

  all = signal<Policy[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  typeFilter = signal('');
  includeInactive = signal(false);

  // Derived state: recomputes automatically when `all` or `typeFilter` change.
  filtered = computed(() => {
    const type = this.typeFilter();
    return type ? this.all().filter((p) => p.policyType === type) : this.all();
  });

  constructor() {
    this.load();
  }

  /** 12 -> "1-year term", 240 -> "20-year term", 1 -> "1-month term" */
  termLabel(months: number): string {
    return months % 12 === 0 ? `${months / 12}-year term` : `${months}-month term`;
  }

  toggleInactive(checked: boolean): void {
    this.includeInactive.set(checked);
    this.load();
  }

  deactivate(p: Policy): void {
    if (!confirm(`Deactivate "${p.policyName}"? Customers will no longer see it.`)) return;
    this.policies.deactivate(p.policyId).subscribe({
      next: () => this.load(),
      error: (err) => this.error.set(getErrorMessage(err)),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.policies.getAll(this.includeInactive()).subscribe({
      next: (list) => {
        this.all.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(getErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
