using Insurance.Catalog.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Enums;

namespace Insurance.Enrollments.Models;

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public PolicyType PolicyType { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyPremium { get; set; }
    public EnrollmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PaymentsPaid { get; set; }
    public int PaymentsTotal { get; set; }
    public int OverdueCount { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public decimal? NextDueAmount { get; set; }
}