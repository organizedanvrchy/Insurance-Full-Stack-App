import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

// Wraps every logged-in page: nav bar on top, the current page in <router-outlet />.
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: "shell.html",
})
export class ShellComponent {
  auth = inject(AuthService);
}
