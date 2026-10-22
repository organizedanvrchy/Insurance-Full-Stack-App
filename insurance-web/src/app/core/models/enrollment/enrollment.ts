import { EnrollmentStatus, PaymentAlert, PolicyType } from '../enums';

export interface Enrollment {
  enrollmentId: number;
  policyId: number;
  policyName: string;
  policyType: PolicyType;
  coverageAmount: number;
  startDate: string;
  endDate: string;
  monthlyPremium: number;
  status: EnrollmentStatus;
  paymentsPaid: number;
  paymentsTotal: number;
  paymentAlert: PaymentAlert;
  overdueCount: number;
  nextDueDate: string | null;
  nextDueAmount: number | null;
}
