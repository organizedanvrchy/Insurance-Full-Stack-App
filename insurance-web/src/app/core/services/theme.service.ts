import { Injectable, effect, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'ins_theme';

/**
 * Holds the current theme and applies it as <html data-theme="...">, which switches the CSS tokens.
 * Until the user clicks the toggle, the theme follows the operating system setting.
 * (index.html sets the first value before Angular starts, to avoid a flash of the wrong theme.)
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private media = window.matchMedia('(prefers-color-scheme: dark)');

  readonly theme = signal<Theme>(this.initial());

  constructor() {
    // Whenever the signal changes, update the attribute the CSS listens to.
    effect(() => document.documentElement.setAttribute('data-theme', this.theme()));

    // Follow OS changes (for example automatic day/night switching) unless
    // the user has chosen.
    this.media.addEventListener('change', (e) => {
      if (!this.stored()) this.theme.set(e.matches ? 'dark' : 'light');
    });
  }

  toggle(): void {
    const next: Theme = this.theme() === 'dark' ? 'light' : 'dark';
    this.theme.set(next);
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      /* storage unavailable (private mode): choice won't persist */
    }
  }

  private stored(): Theme | null {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return value === 'dark' || value === 'light' ? value : null;
    } catch {
      return null;
    }
  }

  private initial(): Theme {
    return this.stored() ?? (this.media.matches ? 'dark' : 'light');
  }
}
