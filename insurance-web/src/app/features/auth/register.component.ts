import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { getErrorMessage } from '../../core/utils/http-error';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle.component';

const passwordsMatch: ValidatorFn = (g: AbstractControl): ValidationErrors | null =>
  g.get('password')?.value === g.get('confirmPassword')?.value ? null : { mismatch: true };

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, ThemeToggleComponent],
  templateUrl: "register.html"
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group(
    {
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      address: [''],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(72)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  get f() {
    return this.form.controls;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    this.auth
      .register({
        fullName: v.fullName,
        email: v.email,
        password: v.password,
        phone: v.phone || undefined,                        // empty string -> omit, so the API's [Phone] check doesn't trip
        address: v.address || undefined,
      })
      .subscribe({
        next: () => this.router.navigate(['/dashboard']),   // API already returned a token = logged in
        error: (err) => {
          this.error.set(getErrorMessage(err));             // e.g. 409 "That email is already registered."
          this.loading.set(false);
        },
      });
  }
}
