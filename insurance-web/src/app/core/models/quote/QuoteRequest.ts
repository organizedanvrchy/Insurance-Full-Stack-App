import { AnswerValue } from "../enums";

export interface QuoteRequest {
  policyId: number;
  answers: Record<string, AnswerValue>;
}
