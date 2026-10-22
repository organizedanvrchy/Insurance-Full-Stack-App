/* An installment is Overdue only once it is MORE than 5 days past its due date.
   To change the grace period, change the 5 here and nowhere else. */
CREATE OR ALTER VIEW dbo.vw_PaymentDetails
AS
SELECT  pp.PaymentId, pp.EnrollmentId, e.UserId,
        pp.DueDate, pp.Amount, pp.PaidDate,
        CASE WHEN pp.Status = 'Pending' AND pp.DueDate < DATEADD(DAY, -5, CAST(SYSUTCDATETIME() AS DATE))
             THEN 'Overdue' ELSE pp.Status END AS Status
FROM dbo.PremiumPayments pp
JOIN dbo.Enrollments e ON e.EnrollmentId = pp.EnrollmentId;
GO

/* usp_Claim_Submit: identical to before except the overdue check now reads the view above.
   Error numbers: 50008 not found/not yours, 50009 not active, 50010 bad incident date,
                  50011 overdue premiums, 50012 amount exceeds remaining coverage */
CREATE OR ALTER PROCEDURE dbo.usp_Claim_Submit
    @UserId       INT,
    @EnrollmentId INT,
    @IncidentDate DATE,
    @Description  NVARCHAR(2000),
    @ClaimAmount  DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @StartDate DATE, @EndDate DATE, @Status NVARCHAR(20), @Coverage DECIMAL(18,2), @Used DECIMAL(18,2);
    DECLARE @Seq BIGINT, @ClaimNumber NVARCHAR(20), @ClaimId INT, @Msg NVARCHAR(200);

    BEGIN TRANSACTION;

        SELECT @StartDate = e.StartDate, @EndDate = e.EndDate, @Status = e.Status, @Coverage = p.CoverageAmount
        FROM dbo.Enrollments e WITH (UPDLOCK)
        JOIN dbo.Policies p ON p.PolicyId = e.PolicyId
        WHERE e.EnrollmentId = @EnrollmentId AND e.UserId = @UserId;

        IF @StartDate IS NULL
            THROW 50008, 'Enrollment not found.', 1;

        IF @Status <> 'Active' OR @EndDate < @Today
            THROW 50009, 'Claims can only be filed on an active policy.', 1;

        IF @IncidentDate < @StartDate OR @IncidentDate > @EndDate OR @IncidentDate > @Today
            THROW 50010, 'The incident date must fall within your coverage period and cannot be in the future.', 1;

        -- Single source of truth for "overdue": the view decides, including the grace period.
        IF EXISTS (SELECT 1 FROM dbo.vw_PaymentDetails
                   WHERE EnrollmentId = @EnrollmentId AND Status = 'Overdue')
            THROW 50011, 'Please pay your overdue premiums before filing a claim.', 1;

        SELECT @Used = ISNULL(SUM(ClaimAmount), 0)
        FROM dbo.Claims
        WHERE EnrollmentId = @EnrollmentId AND Status IN ('Approved', 'Paid');

        IF @ClaimAmount > @Coverage - @Used
        BEGIN
            SET @Msg = CONCAT('The claim amount exceeds your remaining coverage of ',
                              FORMAT(@Coverage - @Used, 'C', 'en-US'), '.');
            THROW 50012, @Msg, 1;
        END

        SET @Seq = NEXT VALUE FOR dbo.ClaimNumberSeq;
        SET @ClaimNumber = CONCAT('CLM-', YEAR(@Today), '-', RIGHT(CONCAT('000000', @Seq), 6));

        INSERT INTO dbo.Claims (ClaimNumber, EnrollmentId, IncidentDate, Description, ClaimAmount)
        VALUES (@ClaimNumber, @EnrollmentId, @IncidentDate, @Description, @ClaimAmount);

        SET @ClaimId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT @ClaimId AS ClaimId;
END
GO