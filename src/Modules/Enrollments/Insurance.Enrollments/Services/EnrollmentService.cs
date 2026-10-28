using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.DTOs;
using Insurance.Enrollments.Enums;
using Insurance.Enrollments.Models;
using Insurance.Enrollments.Repositories;

namespace Insurance.Enrollments.Services;

public class EnrollmentService : IEnrollmentService
{
    // An unpaid installment due within this many days shows a "payment due" alert.
    private const int DueSoonDays = 7;

    private readonly IEnrollmentRepository _enrollments;
    public EnrollmentService(IEnrollmentRepository enrollments) => _enrollments = enrollments;

    public async Task<EnrollmentDto> EnrollAsync(int userId, EnrollmentRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = request.StartDate ?? today;

        // One day of slack: in the evening a customer's local "today" can already be "yesterday" in UTC.
        if (start < today.AddDays(-1))
            throw new BusinessRuleException("Start date cannot be in the past.");
        if (start > today.AddDays(90))
            throw new BusinessRuleException("Start date must be within 90 days from today.");

        // Quote ownership, expiry and one-time use are enforced inside usp_Enrollment_Create.
        var id = await _enrollments.CreateAsync(userId, request.QuoteId, start);

        var created = await _enrollments.GetByIdAsync(id);
        return MapToDto(created!);
    }

    public async Task<List<EnrollmentDto>> GetMineAsync(int userId)
    {
        var list = await _enrollments.GetByUserAsync(userId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<EnrollmentDto?> GetByIdAsync(int enrollmentId, int userId, bool isAdmin)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId);

        // Same null for "doesn't exist" and "belongs to someone else", so ids can't be probed.
        if (enrollment is null || (enrollment.UserId != userId && !isAdmin))
            return null;

        return MapToDto(enrollment);
    }

    // The SQL already checks ownership (WHERE UserId = @UserId), so a stranger's id simply matches nothing.
    public Task<bool> CancelAsync(int enrollmentId, int userId) =>
        _enrollments.CancelAsync(enrollmentId, userId);

    // Overdue comes from the database view (the same rule that blocks claims).
    // "Due soon" is decided here, with the threshold in one place.
    private static PaymentAlert GetAlert(Enrollment e)
    {
        if (e.Status != EnrollmentStatus.Active) return PaymentAlert.None;
        if (e.OverdueCount > 0) return PaymentAlert.Overdue;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (e.NextDueDate is { } due && due <= today.AddDays(DueSoonDays))
            return PaymentAlert.DueSoon;

        return PaymentAlert.None;
    }

    private static EnrollmentDto MapToDto(Enrollment e) => new()
    {
        EnrollmentId = e.EnrollmentId,
        PolicyId = e.PolicyId,
        PolicyName = e.PolicyName,
        PolicyType = e.PolicyType,
        CoverageAmount = e.CoverageAmount,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        MonthlyPremium = e.MonthlyPremium,
        Status = e.Status,
        PaymentsPaid = e.PaymentsPaid,
        PaymentsTotal = e.PaymentsTotal,
        PaymentAlert = GetAlert(e),
        OverdueCount = e.OverdueCount,
        NextDueDate = e.NextDueDate,
        NextDueAmount = e.NextDueAmount
    };
}