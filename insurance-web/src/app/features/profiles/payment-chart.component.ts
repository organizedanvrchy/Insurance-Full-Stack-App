import { Component, computed, input } from '@angular/core';
import { MonthBucket } from '../../core/utils/payment-stats';

type SegmentKind = 'paid' | 'overdue' | 'scheduled';

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

/** Round a raw step up to 1, 2, 2.5, 5 or 10 times a power of ten, so the axis labels look clean. */
function niceStep(raw: number): number {
  if (raw <= 0) return 25;
  const pow = Math.pow(10, Math.floor(Math.log10(raw)));
  const n = raw / pow;
  const f = n <= 1 ? 1 : n <= 2 ? 2 : n <= 2.5 ? 2.5 : n <= 5 ? 5 : 10;
  return f * pow;
}

// Stacked monthly bars drawn as SVG. Colors come from the theme tokens, so it works in light and dark.
@Component({
  selector: 'app-payment-chart',
  templateUrl: "./payment-chart.component.html",
  styleUrl: './payment-chart.component.css',
})
export class PaymentChartComponent {
  buckets = input.required<MonthBucket[]>();

  readonly width = 720;
  readonly height = 280;
  readonly m = { top: 16, right: 8, bottom: 34, left: 56 };
  readonly plotW = this.width - this.m.left - this.m.right;
  readonly plotH = this.height - this.m.top - this.m.bottom;

  fmt = (n: number) => money.format(n);

  maxValue = computed(() => {
    const highest = Math.max(0, ...this.buckets().map((b) => b.total));
    return niceStep(highest / 4) * 4;
  });

  private y = (value: number) => this.m.top + this.plotH - (value / this.maxValue()) * this.plotH;

  ticks = computed(() =>
    [0, 1, 2, 3, 4].map((i) => {
      const value = (this.maxValue() * i) / 4;
      return { value, y: this.y(value), label: '$' + value.toLocaleString('en-US') };
    }),
  );

  bars = computed(() => {
    const data = this.buckets();
    const slot = this.plotW / data.length;
    const barW = Math.min(slot * 0.58, 44);
    const max = this.maxValue();

    return data.map((b, i) => {
      const slotX = this.m.left + i * slot;
      const segments: { kind: SegmentKind; y: number; h: number }[] = [];
      let top = this.m.top + this.plotH;   // stack upward from the baseline

      const parts: [SegmentKind, number][] = [['paid', b.paid], ['overdue', b.overdue], ['scheduled', b.scheduled]];
      for (const [kind, value] of parts) {
        if (value <= 0) continue;
        const h = (value / max) * this.plotH;
        top -= h;
        segments.push({ kind, y: top, h });
      }

      return {
        key: b.key,
        label: b.label,
        current: b.current,
        slotX,
        slotW: slot,
        x: slotX + (slot - barW) / 2,
        w: barW,
        segments,
        tooltip: `${b.title}: paid ${money.format(b.paid)}, scheduled ${money.format(b.scheduled)}, overdue ${money.format(b.overdue)}`,
      };
    });
  });

  ariaLabel = computed(() => {
    const data = this.buckets();
    const sum = (f: (b: MonthBucket) => number) => money.format(data.reduce((n, b) => n + f(b), 0));
    return `Premium installments by due month, ${data[0]?.title} to ${data[data.length - 1]?.title}. Paid ${sum((b) => b.paid)}, scheduled ${sum((b) => b.scheduled)}, overdue ${sum((b) => b.overdue)}.`;
  });
}
