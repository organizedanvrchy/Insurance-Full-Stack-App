import { Payment } from "../models/payment/payment";

export interface MonthBucket {
  key: string;        // "2026-10"
  label: string;      // "Oct"
  title: string;      // "October 2026"
  current: boolean;   // this month
  paid: number;
  overdue: number;
  scheduled: number;  // pending and not yet overdue
  total: number;
}

export interface PaymentSummary {
  totalPaid: number;
  paidCount: number;
  dueSoon: number;          // pending amounts due within the next 30 days
  overdue: number;
  overdueCount: number;
  onTimePercent: number | null;
}

const monthKey = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;

/**
 * Groups installments by DUE month into a fixed window: `monthsBack` months before this month,
 * this month, and `monthsAhead` months after (12 bars by default).
 */
export function buildMonthBuckets(
  payments: Payment[],
  now = new Date(),
  monthsBack = 6,
  monthsAhead = 5,
): MonthBucket[] {
  const buckets: MonthBucket[] = [];
  for (let i = -monthsBack; i <= monthsAhead; i++) {
    const d = new Date(now.getFullYear(), now.getMonth() + i, 1);
    buckets.push({
      key: monthKey(d),
      label: d.toLocaleDateString('en-US', { month: 'short' }),
      title: d.toLocaleDateString('en-US', { month: 'long', year: 'numeric' }),
      current: i === 0,
      paid: 0,
      overdue: 0,
      scheduled: 0,
      total: 0,
    });
  }

  const byKey = new Map(buckets.map((b) => [b.key, b] as const));
  for (const p of payments) {
    const bucket = byKey.get(p.dueDate.slice(0, 7));   // "2026-10-22" -> "2026-10"
    if (!bucket) continue;                              // outside the window
    if (p.status === 'Paid') bucket.paid += p.amount;
    else if (p.status === 'Overdue') bucket.overdue += p.amount;
    else bucket.scheduled += p.amount;
  }
  for (const b of buckets) b.total = b.paid + b.overdue + b.scheduled;
  return buckets;
}

export function summarizePayments(payments: Payment[], now = new Date()): PaymentSummary {
  const cutoff = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 30).toLocaleDateString('en-CA'); // YYYY-MM-DD

  let totalPaid = 0, paidCount = 0, onTime = 0, dueSoon = 0, overdue = 0, overdueCount = 0;
  for (const p of payments) {
    if (p.status === 'Paid') {
      totalPaid += p.amount;
      paidCount++;
      if (p.paidDate && p.paidDate <= p.dueDate) onTime++;   // ISO dates compare correctly as text
    } else if (p.status === 'Overdue') {
      overdue += p.amount;
      overdueCount++;
    } else if (p.dueDate <= cutoff) {
      dueSoon += p.amount;
    }
  }

  return {
    totalPaid,
    paidCount,
    dueSoon,
    overdue,
    overdueCount,
    onTimePercent: paidCount > 0 ? Math.round((onTime / paidCount) * 100) : null,
  };
}
