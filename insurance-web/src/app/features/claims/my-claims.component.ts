import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ClaimService } from '../../core/services/claim.service';
import { getErrorMessage } from '../../core/utils/http-error';
import { claimBadgeClass, statusLabel } from '../../core/utils/labels';
import { Claim } from '../../core/models/claim/claim';

@Component({
  selector: 'app-my-claims',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: "my-claims.html",
})
export class MyClaimsComponent {
  private api = inject(ClaimService);

  readonly label = statusLabel;
  readonly badgeClass = claimBadgeClass;

  claims = signal<Claim[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor() {
    this.api.getMine().subscribe({
      next: (list) => {
        this.claims.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(getErrorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
