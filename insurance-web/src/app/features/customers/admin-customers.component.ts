import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Customer } from '../../core/models/customer/customer';
import { CustomerService } from '../../core/services/customer.service';
import { getErrorMessage } from '../../core/utils/http-error';

type StatusFilter = '' | 'active' | 'inactive';

@Component({
  selector: 'app-admin-customers',
  imports: [DatePipe],
  templateUrl: "admin-customers.html",
  styleUrl: "admin-customers.component.css",
})
export class AdminCustomersComponent {
  private api = inject(CustomerService);

  customers = signal<Customer[]>([]);
  loading = signal(true);
  busyId = signal<number | null>(null);
  error = signal<string | null>(null);

  private search = '';
  private status: StatusFilter = '';

  constructor() {
    this.load();
  }

  applySearch(value: string): void {
    this.search = value.trim();
    this.load();
  }

  setStatus(value: string): void {
    this.status = value as StatusFilter;
    this.load();
  }

  toggle(c: Customer): void {
    const message = c.isActive
      ? `Deactivate ${c.fullName}? They won't be able to log in. Their policies and claims are kept.`
      : `Reactivate ${c.fullName}?`;
    if (!confirm(message)) return;

    this.busyId.set(c.userId);
    this.error.set(null);

    this.api.setActive(c.userId, !c.isActive).subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();   // reload so the active/inactive filter stays accurate
      },
      error: (err) => {
        this.error.set(getErrorMessage(err));
        this.busyId.set(null);
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    const isActive = this.status === '' ? undefined : this.status === 'active';
    this.api.getAll(this.search || undefined, isActive).subscribe({
      next: (list) => {
        this.customers.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(getErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
