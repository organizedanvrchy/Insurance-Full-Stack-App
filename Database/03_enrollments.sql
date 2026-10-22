/* A VIEW is a saved SELECT. Both read procs share it, so the join and the "derived Expired" rule live in one place. */
CREATE OR ALTER VIEW dbo.vw_EnrollmentDetails
AS
SELECT  e.EnrollmentId, e.UserId, e.PolicyId,
        p.PolicyName, p.PolicyType, p.CoverageAmount,
        e.StartDate, e.EndDate, e.MonthlyPremium,
        -- Nothing flips Status to 'Expired' automatically, so we derive it when reading.
        CASE WHEN e.Status = 'Active' AND e.EndDate < CAST(SYSUTCDATETIME() AS DATE)
             THEN 'Expired' ELSE e.Status END AS Status,
        e.CreatedAt,
        (SELECT COUNT(*) FROM dbo.PremiumPayments pp WHERE pp.EnrollmentId = e.EnrollmentId AND pp.Status = 'Paid') AS PaymentsPaid,
        (SELECT COUNT(*) FROM dbo.PremiumPayments pp WHERE pp.EnrollmentId = e.EnrollmentId)                       AS PaymentsTotal
FROM dbo.Enrollments e
JOIN dbo.Policies p ON p.PolicyId = e.PolicyId;
GO

/* THE TRANSACTION PROC: enrollment row + all premium rows succeed together or not at all. */
CREATE OR ALTER PROCEDURE dbo.usp_Enrollment_Create
    @UserId    INT,
    @PolicyId  INT,
    @StartDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;     -- any runtime error automatically rolls the whole transaction back

    DECLARE @Premium DECIMAL(18,2), @Term INT, @EnrollmentId INT;

    SELECT @Premium = MonthlyPremium, @Term = TermMonths
    FROM dbo.Policies
    WHERE PolicyId = @PolicyId AND IsActive = 1;

    IF @Premium IS NULL
        THROW 50003, 'That policy does not exist or is no longer available.', 1;

    BEGIN TRANSACTION;

        -- UPDLOCK + HOLDLOCK: two simultaneous requests can't both pass this check.
        IF EXISTS (SELECT 1 FROM dbo.Enrollments WITH (UPDLOCK, HOLDLOCK)
                   WHERE UserId = @UserId AND PolicyId = @PolicyId
                     AND Status = 'Active' AND EndDate >= CAST(SYSUTCDATETIME() AS DATE))
            THROW 50002, 'You already have an active enrollment in this policy.', 1;

        INSERT INTO dbo.Enrollments (UserId, PolicyId, StartDate, EndDate, MonthlyPremium)
        VALUES (@UserId, @PolicyId, @StartDate, DATEADD(MONTH, @Term, @StartDate), @Premium);

        SET @EnrollmentId = SCOPE_IDENTITY();

        -- One premium row per month. Counting 0..Term-1 and adding N months to the START date
        -- (not to the previous due date) keeps end-of-month starts correct: Jan 31, Feb 28, Mar 31...
        WITH Months AS (
            SELECT 0 AS N
            UNION ALL
            SELECT N + 1 FROM Months WHERE N + 1 < @Term
        )
        INSERT INTO dbo.PremiumPayments (EnrollmentId, DueDate, Amount)
        SELECT @EnrollmentId, DATEADD(MONTH, N, @StartDate), @Premium
        FROM Months
        OPTION (MAXRECURSION 600);      -- default limit is 100; our max term is 600 months

    COMMIT TRANSACTION;

    SELECT @EnrollmentId AS EnrollmentId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Enrollment_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_EnrollmentDetails
    WHERE UserId = @UserId
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Enrollment_GetById
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_EnrollmentDetails
    WHERE EnrollmentId = @EnrollmentId;
END
GO

/* Cancel: only the owner, only while Active. Unpaid installments are removed; paid ones stay as history. */
CREATE OR ALTER PROCEDURE dbo.usp_Enrollment_Cancel
    @EnrollmentId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Updated INT;

    BEGIN TRANSACTION;

        UPDATE dbo.Enrollments
        SET Status = 'Cancelled'
        WHERE EnrollmentId = @EnrollmentId AND UserId = @UserId AND Status = 'Active';

        SET @Updated = @@ROWCOUNT;

        IF @Updated = 1
            DELETE FROM dbo.PremiumPayments
            WHERE EnrollmentId = @EnrollmentId AND PaidDate IS NULL;

    COMMIT TRANSACTION;

    SELECT @Updated AS RowsUpdated;
END
GO
