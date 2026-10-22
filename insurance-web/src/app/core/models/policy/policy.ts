import { PolicyType } from '../enums';

export interface Policy {
  policyId: number;
  policyName: string;
  policyType: PolicyType;
  description: string | null;
  coverageAmount: number;
  monthlyPremium: number;
  termMonths: number;
  isActive: boolean;
  createdAt: string;
}
