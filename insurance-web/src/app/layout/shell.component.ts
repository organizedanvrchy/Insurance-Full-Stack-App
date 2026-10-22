import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { ThemeToggleComponent } from '../shared/components/theme-toggle.component';
import { BrandLogoComponent } from '../shared/components/brand-logo.component';
import { BRAND } from '../core/brand';

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
}
