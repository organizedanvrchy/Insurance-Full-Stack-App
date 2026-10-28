using Insurance.Billing.DTOs;
using Insurance.Billing.Models;
using Insurance.Billing.Repositories;
using Insurance.Enrollments.Contracts;

namespace Insurance.Billing.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _payments;
    private readonly IEnrollmentLookup _enrollments;

    public PaymentService(IPaymentRepository payments, IEnrollmentLookup enrollments)
    {
        _payments = payments;
        _enrollments = enrollments;
    }

    public async Task<List<PaymentDto>?> GetScheduleAsync(int enrollmentId, int userId, bool isAdmin)
    {
        var enrollment = await _enrollments.GetAsync(enrollmentId);

        // Same null for "doesn't exist" and "not yours".
        if (enrollment is null || (enrollment.UserId != userId && !isAdmin))
            return null;

        var payments = await _payments.GetByEnrollmentAsync(enrollmentId);
        return payments.Select(MapToDto).ToList();
    }

    // Ownership and all payment rules are enforced inside usp_Payment_Pay (one transaction).
    public async Task<PaymentDto> PayAsync(int paymentId, int userId) =>
        MapToDto(await _payments.PayAsync(paymentId, userId));

    private static PaymentDto MapToDto(Payment p) => new()
    {
        PaymentId = p.PaymentId,
        EnrollmentId = p.EnrollmentId,
        DueDate = p.DueDate,
        Amount = p.Amount,
        PaidDate = p.PaidDate,
        Status = p.Status
    };
}