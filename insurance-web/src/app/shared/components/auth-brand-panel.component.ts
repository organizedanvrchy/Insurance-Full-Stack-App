import { Component } from '@angular/core';
import { BRAND } from '../../core/brand';
import { BrandLogoComponent } from './brand-logo.component';

// The branded side of the sign-in and register pages.
@Component({
  selector: 'app-auth-brand-panel',
  imports: [BrandLogoComponent],
  templateUrl: "./auth-brand-panel.component.html",
  styleUrl: './auth-brand-panel.component.css',
})
export class AuthBrandPanelComponent {
  readonly slogan = BRAND.slogan;

  readonly points = [
    'Transparent quotes: see exactly how your price is built',
    'Pay on your schedule, with a heads-up before anything is due',
    'File and track claims online, with clear updates at every step',
  ];

  // Fixed values (not random) so the rain looks the same on every visit.
  // Negative delays mean the rain is already falling when the page loads.
  readonly drops = [
    { left: 5, delay: -0.4, duration: 2.8 },
    { left: 12, delay: -2.1, duration: 3.4 },
    { left: 19, delay: -1.2, duration: 2.5 },
    { left: 27, delay: -2.9, duration: 3.1 },
    { left: 34, delay: -0.8, duration: 2.7 },
    { left: 42, delay: -1.9, duration: 3.6 },
    { left: 49, delay: -0.1, duration: 2.4 },
    { left: 57, delay: -2.5, duration: 3.2 },
    { left: 64, delay: -1.5, duration: 2.9 },
    { left: 72, delay: -0.6, duration: 3.5 },
    { left: 79, delay: -2.3, duration: 2.6 },
    { left: 86, delay: -1.0, duration: 3.3 },
    { left: 93, delay: -1.7, duration: 2.8 },
  ];
}
