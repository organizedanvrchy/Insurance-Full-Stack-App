import { PolicyType } from "../enums";

export interface PolicyRequest {
  policyName: string;
  policyType: PolicyType;
  description?: string;
  coverageAmount: number;
  monthlyPremium: number;
  termMonths: number;
}
