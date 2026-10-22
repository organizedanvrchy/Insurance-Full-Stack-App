/* Same view as before, plus: OverdueCount, NextDueDate, NextDueAmount.
   Overdue comes from vw_PaymentDetails, the single source of truth (including the grace period). */
CREATE OR ALTER VIEW dbo.vw_EnrollmentDetails
AS
SELECT  e.EnrollmentId, e.UserId, e.PolicyId,
        p.PolicyName, p.PolicyType, p.CoverageAmount,
        e.StartDate, e.EndDate, e.MonthlyPremium,
        CASE WHEN e.Status = 'Active' AND e.EndDate < CAST(SYSUTCDATETIME() AS DATE)
             THEN 'Expired' ELSE e.Status END AS Status,
        e.CreatedAt,
        (SELECT COUNT(*) FROM dbo.PremiumPayments pp WHERE pp.EnrollmentId = e.EnrollmentId AND pp.Status = 'Paid') AS PaymentsPaid,
        (SELECT COUNT(*) FROM dbo.PremiumPayments pp WHERE pp.EnrollmentId = e.EnrollmentId)                       AS PaymentsTotal,
        (SELECT COUNT(*) FROM dbo.vw_PaymentDetails pd WHERE pd.EnrollmentId = e.EnrollmentId AND pd.Status = 'Overdue') AS OverdueCount,
        nxt.DueDate AS NextDueDate,        -- earliest unpaid installment (NULL if none left)
        nxt.Amount  AS NextDueAmount
FROM dbo.Enrollments e
JOIN dbo.Policies p ON p.PolicyId = e.PolicyId
OUTER APPLY (
    SELECT TOP (1) pp.DueDate, pp.Amount
    FROM dbo.PremiumPayments pp
    WHERE pp.EnrollmentId = e.EnrollmentId AND pp.PaidDate IS NULL
    ORDER BY pp.DueDate
) nxt;
GO