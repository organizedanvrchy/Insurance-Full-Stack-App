using Insurance.Billing.Contracts;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.DTOs;
using Insurance.Enrollments.Enums;
using Insurance.Enrollments.Models;
using Insurance.Enrollments.Repositories;
using Insurance.Quotes.Contracts;

namespace Insurance.Enrollments.Services;

public class EnrollmentService : IEnrollmentService
{
    // An unpaid installment due within this many days shows a "payment due" alert.
    private const int DueSoonDays = 7;

    private readonly IEnrollmentRepository _enrollments;
    private readonly IQuoteRedemption _quotes;
    private readonly IPlanCatalog _plans;
    private readonly IPremiumSchedule _premiums;

    public EnrollmentService(IEnrollmentRepository enrollments, IQuoteRedemption quotes, IPlanCatalog plans, IPremiumSchedule premiums)
    {
        _enrollments = enrollments;
        _quotes = quotes;
        _plans = plans;
        _premiums = premiums;
    }

    // Enrolling spans four modules, so it runs as steps, each committed by its own module:
    // check the quote -> check the plan -> create the enrollment -> create the premium schedule -> use up the quote.
    // If a step after the enrollment fails, the earlier steps are undone (compensated) before the error is rethrown.
    public async Task<EnrollmentDto> EnrollAsync(int userId, EnrollmentRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = request.StartDate ?? today;

        // One day of slack: in the evening a customer's local "today" can already be "yesterday" in UTC.
        if (start < today.AddDays(-1))
            throw new BusinessRuleException("Start date cannot be in the past.");
        if (start > today.AddDays(90))
            throw new BusinessRuleException("Start date must be within 90 days from today.");

        // Ownership, expiry and one-time use. The premium and the plan come from the quote, so the caller can't choose either.
        var quote = await _quotes.GetForEnrollmentAsync(request.QuoteId, userId);

        var plan = await _plans.GetAsync(quote.PolicyId);
        if (plan is null || !plan.IsActive)
            throw new BusinessRuleException("That policy does not exist or is no longer available.");

        // "One active enrollment per plan" is enforced inside usp_Enrollment_Create, with a lock.
        var id = await _enrollments.CreateAsync(userId, plan.PolicyId, start, plan.TermMonths, quote.MonthlyPremium);

        try
        {
            await _premiums.CreateAsync(id, start, plan.TermMonths, quote.MonthlyPremium);
            await _quotes.MarkUsedAsync(quote.QuoteId, id);
        }
        catch
        {
            await _premiums.RemoveUnpaidAsync(id);
            await _enrollments.DeleteAsync(id);
            throw;
        }

        var created = await _enrollments.GetByIdAsync(id);
        return (await ComposeAsync(new[] { created! })).Single();
    }

    public async Task<List<EnrollmentDto>> GetMineAsync(int userId)
    {
        var list = await _enrollments.GetByUserAsync(userId);
        return await ComposeAsync(list);
    }

    public async Task<EnrollmentDto?> GetByIdAsync(int enrollmentId, int userId, bool isAdmin)
    {
        var enrollment = await _enrollments.GetByIdAsync(enrollmentId);

        // Same null for "doesn't exist" and "belongs to someone else", so ids can't be probed.
        if (enrollment is null || (enrollment.UserId != userId && !isAdmin))
            return null;

        return (await ComposeAsync(new[] { enrollment })).Single();
    }

    // The SQL checks ownership (WHERE UserId = @UserId), so a stranger's id simply matches nothing.
    // Unpaid installments are removed by Billing; paid ones stay as history.
    public async Task<bool> CancelAsync(int enrollmentId, int userId)
    {
        var cancelled = await _enrollments.CancelAsync(enrollmentId, userId);
        if (cancelled)
            await _premiums.RemoveUnpaidAsync(enrollmentId);
        return cancelled;
    }

    // Adds the plan details (Catalog) and payment progress (Billing), with one call to each for the whole list.
    private async Task<List<EnrollmentDto>> ComposeAsync(IReadOnlyCollection<Enrollment> enrollments)
    {
        if (enrollments.Count == 0) return new List<EnrollmentDto>();

        var plans = await _plans.GetManyAsync(enrollments.Select(e => e.PolicyId));
        var premiums = await _premiums.GetSummariesAsync(enrollments.Select(e => e.EnrollmentId));

        return enrollments.Select(e => MapToDto(e, plans[e.PolicyId], premiums[e.EnrollmentId])).ToList();
    }

    // Overdue comes from Billing (the same rule that blocks claims).
    // "Due soon" is decided here, with the threshold in one place.
    private static PaymentAlert GetAlert(Enrollment e, PremiumSummary p)
    {
        if (e.Status != EnrollmentStatus.Active) return PaymentAlert.None;
        if (p.OverdueCount > 0) return PaymentAlert.Overdue;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (p.NextDueDate is { } due && due <= today.AddDays(DueSoonDays))
            return PaymentAlert.DueSoon;

        return PaymentAlert.None;
    }

    private static EnrollmentDto MapToDto(Enrollment e, PlanInfo plan, PremiumSummary p) => new()
    {
        EnrollmentId = e.EnrollmentId,
        PolicyId = e.PolicyId,
        PolicyName = plan.PolicyName,
        PolicyType = plan.PolicyType,
        CoverageAmount = plan.CoverageAmount,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        MonthlyPremium = e.MonthlyPremium,
        Status = e.Status,
        PaymentsPaid = p.PaymentsPaid,
        PaymentsTotal = p.PaymentsTotal,
        PaymentAlert = GetAlert(e, p),
        OverdueCount = p.OverdueCount,
        NextDueDate = p.NextDueDate,
        NextDueAmount = p.NextDueAmount
    };
}