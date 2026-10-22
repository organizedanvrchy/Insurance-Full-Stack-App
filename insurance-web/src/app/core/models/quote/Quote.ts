import { PolicyType } from "../enums";
import { RatingFactor } from "./RatingFactor";

export interface Quote {
  quoteId: number;
  policyId: number;
  policyName: string;
  policyType: PolicyType;
  coverageAmount: number;
  termMonths: number;
  basePremium: number;
  monthlyPremium: number;
  totalOverTerm: number;
  factors: RatingFactor[];
  expiresAt: string;
  isExpired: boolean;
  isUsed: boolean;
}
