using Insurance.Enrollments.Contracts;

namespace Insurance.Enrollments.Models;

// Only what this service stores. Plan details come from the Catalog service and payment counts from Billing.
public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }
    public int PolicyId { get; set; }
    public int QuoteId { get; set; }                  // the saga needs it to confirm or release the quote
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyPremium { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
