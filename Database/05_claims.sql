USE InsuranceDb;
GO

/* Claim numbers come from a SEQUENCE: a counter owned by the database. Rolled-back inserts can leave gaps; that's normal. */
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'ClaimNumberSeq')
    CREATE SEQUENCE dbo.ClaimNumberSeq AS BIGINT START WITH 1 INCREMENT BY 1;
GO

CREATE OR ALTER VIEW dbo.vw_ClaimDetails
AS
SELECT  c.ClaimId, c.ClaimNumber, c.EnrollmentId, e.UserId,
        u.FullName AS CustomerName, u.Email AS CustomerEmail,
        p.PolicyName, p.PolicyType,
        c.IncidentDate, c.Description, c.ClaimAmount, c.Status, c.AdminNotes,
        c.SubmittedAt, c.ReviewedAt, r.FullName AS ReviewedByName
FROM dbo.Claims c
JOIN dbo.Enrollments e ON e.EnrollmentId = c.EnrollmentId
JOIN dbo.Users u       ON u.UserId       = e.UserId
JOIN dbo.Policies p    ON p.PolicyId     = e.PolicyId
LEFT JOIN dbo.Users r  ON r.UserId       = c.ReviewedByUserId;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Claim_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_ClaimDetails
    WHERE UserId = @UserId
    ORDER BY SubmittedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Claim_GetAll
    @Status NVARCHAR(20) = NULL      -- NULL = every status
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_ClaimDetails
    WHERE @Status IS NULL OR Status = @Status
    ORDER BY SubmittedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Claim_GetById
    @ClaimId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_ClaimDetails WHERE ClaimId = @ClaimId;
END
GO

/* Submit a claim. Every rule is checked inside one transaction, with the enrollment row locked.
   Error numbers:
     50008 enrollment not found OR not yours
     50009 enrollment not active
     50010 incident date outside the coverage period / in the future
     50011 overdue premiums
     50012 amount exceeds remaining coverage */
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

        IF EXISTS (SELECT 1 FROM dbo.PremiumPayments
                   WHERE EnrollmentId = @EnrollmentId AND PaidDate IS NULL AND DueDate < @Today)
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
        SET @ClaimNumber = CONCAT('CLM-', YEAR(@Today), '-', RIGHT(CONCAT('000000', @Seq), 6));   -- e.g. CLM-2026-000042

        INSERT INTO dbo.Claims (ClaimNumber, EnrollmentId, IncidentDate, Description, ClaimAmount)
        VALUES (@ClaimNumber, @EnrollmentId, @IncidentDate, @Description, @ClaimAmount);

        SET @ClaimId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT @ClaimId AS ClaimId;
END
GO

/* Move a claim to a new status.
   @FromStatus is the status the caller THINKS the claim is in. If it changed meanwhile, the UPDATE matches no row.
   Error numbers:
     50013 claim changed by someone else (stale @FromStatus)
     50014 claim not found
     50015 approving would exceed remaining coverage */
CREATE OR ALTER PROCEDURE dbo.usp_Claim_Review
    @ClaimId    INT,
    @FromStatus NVARCHAR(20),
    @ToStatus   NVARCHAR(20),
    @AdminNotes NVARCHAR(1000) = NULL,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EnrollmentId INT, @Amount DECIMAL(18,2), @Coverage DECIMAL(18,2), @Used DECIMAL(18,2);

    BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Claims WITH (UPDLOCK) WHERE ClaimId = @ClaimId)
            THROW 50014, 'Claim not found.', 1;

        UPDATE dbo.Claims
        SET Status           = @ToStatus,
            AdminNotes       = COALESCE(@AdminNotes, AdminNotes),   -- keep earlier notes if none supplied
            ReviewedAt       = SYSUTCDATETIME(),
            ReviewedByUserId = @ReviewerId
        WHERE ClaimId = @ClaimId AND Status = @FromStatus;

        IF @@ROWCOUNT = 0
            THROW 50013, 'This claim was changed by someone else. Please refresh and try again.', 1;

        IF @ToStatus = 'Approved'
        BEGIN
            -- Lock the enrollment so two approvals for the same policy can't both pass this check.
            SELECT @EnrollmentId = c.EnrollmentId, @Amount = c.ClaimAmount, @Coverage = p.CoverageAmount
            FROM dbo.Claims c
            JOIN dbo.Enrollments e WITH (UPDLOCK) ON e.EnrollmentId = c.EnrollmentId
            JOIN dbo.Policies p ON p.PolicyId = e.PolicyId
            WHERE c.ClaimId = @ClaimId;

            SELECT @Used = ISNULL(SUM(ClaimAmount), 0)
            FROM dbo.Claims
            WHERE EnrollmentId = @EnrollmentId AND Status IN ('Approved', 'Paid') AND ClaimId <> @ClaimId;

            IF @Amount > @Coverage - @Used
                THROW 50015, 'Approving this claim would exceed the remaining coverage on this policy.', 1;
        END

    COMMIT TRANSACTION;

    SELECT * FROM dbo.vw_ClaimDetails WHERE ClaimId = @ClaimId;
END
GO