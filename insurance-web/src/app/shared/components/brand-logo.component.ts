import { Component, input } from '@angular/core';

// Umbrella mark + "RAINsurance" wordmark. Colors come from CSS variables, so it works in both themes.
// A parent can override --logo-mark, --logo-text and --logo-accent (the sign-in panel does).
@Component({
  selector: 'app-brand-logo',
  template: `
    <span class="logo" [class.large]="size() === 'lg'">
      <svg class="mark" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
        <path class="canopy" d="M4 16C4 9.37 9.37 4 16 4s12 5.37 12 12H4z" />
        <path class="handle" d="M16 16v8.5a3 3 0 0 1-6 0" />
        <path class="drop" d="M22 19.5c1.3 1.8 2 3 2 4a2 2 0 0 1-4 0c0-1 .7-2.2 2-4z" />
        <path class="drop" d="M27 22c.9 1.2 1.4 2 1.4 2.7a1.4 1.4 0 0 1-2.8 0c0-.7.5-1.5 1.4-2.7z" />
      </svg>
      <span class="word"><span class="rain">RAIN</span>surance</span>
    </span>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
      }
      .logo {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 1.15rem;
        font-weight: 800;
        letter-spacing: -0.01em;
        line-height: 1;
      }
      .logo.large {
        font-size: 1.9rem;
        gap: 0.65rem;
      }
      .mark {
        width: 1.7em;
        height: 1.7em;
      }
      .canopy {
        fill: var(--logo-mark, var(--primary));
      }
      .handle {
        fill: none;
        stroke: var(--logo-mark, var(--primary));
        stroke-width: 2.4;
        stroke-linecap: round;
      }
      .drop {
        fill: var(--logo-accent, var(--accent));
      }
      .word {
        color: var(--logo-text, var(--text));
      }
      .rain {
        color: var(--logo-accent, var(--accent));
      }
    `,
  ],
})
export class BrandLogoComponent {
  size = input<'md' | 'lg'>('md');
}
