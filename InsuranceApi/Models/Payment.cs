using InsuranceApi.Enums;

namespace InsuranceApi.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public int EnrollmentId { get; set; }
    public int UserId { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? PaidDate { get; set; }
    public PaymentStatus Status { get; set; }
}