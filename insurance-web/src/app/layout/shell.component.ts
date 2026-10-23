import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { BRAND } from '../core/brand';
import { AuthService } from '../core/services/auth.service';
import { initialsOf } from '../core/utils/initials';
import { BrandLogoComponent } from '../shared/components/brand-logo.component';
import { ThemeToggleComponent } from '../shared/components/theme-toggle.component';

// Wraps every logged-in page: nav bar on top, the current page in <router-outlet />.
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ThemeToggleComponent, BrandLogoComponent],
  templateUrl: "shell.html",
  styleUrl: "./shell-component.css",
})
export class ShellComponent {
  auth = inject(AuthService);

  readonly name = BRAND.name;
  readonly tagline = BRAND.shortSlogan;
  readonly year = new Date().getFullYear();

  initials = computed(() => initialsOf(this.auth.user()?.fullName));

  // Fixed (not random) values so the rain looks the same on every visit.
  // Negative delays mean the rain is already falling when the page loads.
  readonly drops = Array.from({ length: 44 }, (_, i) => ({
    left: (i * 23.7) % 100,
    delay: -(((i * 37) % 19) / 10),
    duration: 1.2 + ((i * 53) % 10) / 10,
    length: 10 + ((i * 29) % 12),
  }));
}
