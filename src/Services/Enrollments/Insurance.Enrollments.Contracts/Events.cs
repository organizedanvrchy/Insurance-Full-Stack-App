namespace Insurance.Enrollments.Contracts;

// Events published by the Enrollments service. The enrollment saga:
//
//   Enrollments: quote reserved (Quotes, HTTP) -> enrollment saved as Pending -> EnrollmentRequested
//   Billing:     creates the premium schedule  -> PremiumScheduleCreated   (or a Fault<EnrollmentRequested>)
//   Enrollments: Pending -> Active             -> EnrollmentActivated      (or Pending -> Failed -> EnrollmentFailed)
//   Quotes:      marks the quote used on EnrollmentActivated, releases it on EnrollmentFailed
//   Billing:     removes the schedule on EnrollmentFailed

/// <summary>An enrollment was saved as Pending and needs its premium schedule.</summary>
public record EnrollmentRequested(int EnrollmentId, int UserId, DateOnly StartDate, int TermMonths, decimal MonthlyPremium);

/// <summary>The enrollment saga finished: the policy is active.</summary>
public record EnrollmentActivated(int EnrollmentId, int UserId, int PolicyId, int QuoteId);

/// <summary>The enrollment saga could not finish. Undo what was done for it.</summary>
public record EnrollmentFailed(int EnrollmentId, int QuoteId);

/// <summary>The customer cancelled the policy. Unpaid installments should go; paid ones stay as history.</summary>
public record EnrollmentCancelled(int EnrollmentId, int UserId);
