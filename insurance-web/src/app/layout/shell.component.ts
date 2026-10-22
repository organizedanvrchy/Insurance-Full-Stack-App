import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { ThemeToggleComponent } from '../shared/components/theme-toggle.component';

// Wraps every logged-in page: nav bar on top, the current page in <router-outlet />.
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ThemeToggleComponent],
  templateUrl: "shell.html",
  styleUrl: "shell-component.css",
})
export class ShellComponent {
  auth = inject(AuthService);
}
