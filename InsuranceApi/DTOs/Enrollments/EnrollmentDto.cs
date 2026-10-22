using InsuranceApi.Enums;

namespace InsuranceApi.DTOs;

public class EnrollmentDto
{
    public int EnrollmentId { get; set; }
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public PolicyType PolicyType { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal MonthlyPremium { get; set; }
    public EnrollmentStatus Status { get; set; }
    public int PaymentsPaid { get; set; }
    public int PaymentsTotal { get; set; }

    // The server decides what needs attention; the UI just displays it.
    public PaymentAlert PaymentAlert { get; set; }
    public int OverdueCount { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public decimal? NextDueAmount { get; set; }
}