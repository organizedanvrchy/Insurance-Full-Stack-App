import { Component, inject } from '@angular/core';
import { ThemeService } from '../../core/services/theme.service';

// One button that switches between the light and dark theme. Used in the app shell and on the public pages.
@Component({
  selector: 'app-theme-toggle',
  templateUrl: "theme-toggle.component.html",
})
export class ThemeToggleComponent {
  theme = inject(ThemeService);
}
