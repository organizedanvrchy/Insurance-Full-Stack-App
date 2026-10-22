import { PaymentStatus } from '../enums';

// Mirrors Api/DTOs/Payments/PaymentDto.cs. Dates arrive as "2026-11-01" strings; paidDate is null until paid.
export interface Payment {
  paymentId: number;
  enrollmentId: number;
  dueDate: string;
  amount: number;
  paidDate: string | null;
  status: PaymentStatus;
}
