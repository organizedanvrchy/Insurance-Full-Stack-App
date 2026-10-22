import { ClaimStatus } from "../enums";

export interface ClaimReviewRequest {
  status: ClaimStatus;
  adminNotes?: string;
}
