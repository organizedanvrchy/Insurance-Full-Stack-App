namespace Insurance.Billing.Contracts;

/// <summary>Premium schedules, for the Enrollments and Claims modules.</summary>
public interface IPremiumSchedule
{
    /// <summary>One installment per month of the term, starting on the start date.</summary>
    Task CreateAsync(int enrollmentId, DateOnly startDate, int termMonths, decimal monthlyPremium);

    /// <summary>Removes the unpaid installments (cancelling a policy). Paid ones stay as history.</summary>
    Task RemoveUnpaidAsync(int enrollmentId);

    /// <summary>One entry per requested enrollment, even if it has no installments.</summary>
    Task<IReadOnlyDictionary<int, PremiumSummary>> GetSummariesAsync(IEnumerable<int> enrollmentIds);
}

/// <summary>Overdue uses the grace period. NextDue is the earliest unpaid installment, if any.</summary>
public record PremiumSummary(
    int PaymentsPaid,
    int PaymentsTotal,
    int OverdueCount,
    DateOnly? NextDueDate,
    decimal? NextDueAmount);
