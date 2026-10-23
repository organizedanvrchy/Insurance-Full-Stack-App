import { Component, computed, inject } from '@angular/core';
import { ThemeService } from '../../core/services/theme.service';

// One button that switches between the light and dark theme. Used in the app shell and on the public pages.
@Component({
  selector: 'app-theme-toggle',
  templateUrl: "./theme-toggle.component.html",
  styleUrl: "./theme-toggle.component.css"
})
export class ThemeToggleComponent {
  theme = inject(ThemeService);

  isDark = computed(() => this.theme.theme() === 'dark');
  label = computed(() => (this.isDark() ? 'Switch to light theme' : 'Switch to dark theme'));
}
