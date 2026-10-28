using Insurance.Enrollments.Contracts;

namespace Insurance.Enrollments.Models;

// Only what this module stores. Plan details come from Catalog and payment counts from Billing.
public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }
    public int PolicyId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyPremium { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
