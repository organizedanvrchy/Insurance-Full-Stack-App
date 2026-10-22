import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { POLICY_TYPES, PolicyType } from '../../core/models/enums';
import { PolicyService } from '../../core/services/policy.service';
import { getErrorMessage } from '../../core/utils/http-error';
import { PolicyRequest } from '../../core/models/policy/policyRequest';

// One component for both "New" (/policies/new) and "Edit" (/policies/:id/edit).
@Component({
  selector: 'app-policy-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: "policy-form.html"
})
export class PolicyFormComponent {
  private fb = inject(FormBuilder);
  private policies = inject(PolicyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly policyTypes = POLICY_TYPES;
  readonly id: number | null = Number(this.route.snapshot.paramMap.get('id')) || null;

  saving = signal(false);
  error = signal<string | null>(null);

  // Validators mirror the API's rules for fast feedback; the API still enforces them for real.
  form = this.fb.group({
    policyName: ['', [Validators.required, Validators.maxLength(100)]],
    policyType: ['' as PolicyType | '', Validators.required],
    description: ['' as string | null, Validators.maxLength(1000)],
    coverageAmount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    monthlyPremium: [null as number | null, [Validators.required, Validators.min(0.01)]],
    termMonths: [null as number | null, [Validators.required, Validators.min(1), Validators.max(600)]],
  });

  get f() {
    return this.form.controls;
  }

  constructor() {
    if (this.id) {
      this.policies.getById(this.id).subscribe({
        next: (p) => this.form.patchValue(p),   // fills matching fields, ignores the rest
        error: (err) => this.error.set(getErrorMessage(err)),
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    const body: PolicyRequest = {
      policyName: v.policyName!,
      policyType: v.policyType as PolicyType,
      description: v.description || undefined,
      coverageAmount: v.coverageAmount!,
      monthlyPremium: v.monthlyPremium!,
      termMonths: v.termMonths!,
    };

    const request$: Observable<unknown> = this.id
      ? this.policies.update(this.id, body)
      : this.policies.create(body);

    request$.subscribe({
      next: () => this.router.navigate(['/policies']),
      error: (err) => {
        this.error.set(getErrorMessage(err));
        this.saving.set(false);
      },
    });
  }
}
