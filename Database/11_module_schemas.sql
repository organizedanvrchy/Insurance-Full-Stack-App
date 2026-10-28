/* MODULE SCHEMAS: step 1 toward microservices.

   Each API module owns one schema, and its procedures and views touch ONLY that schema:

     accounts     Identity module     Users
     catalog      Catalog module      Policies
     quotes       Quotes module       Quotes
     enrollments  Enrollments module  Enrollments
     billing      Billing module      PremiumPayments
     claims       Claims module       Claims, ClaimNumberSeq

   Anything that used to JOIN across these (claim details, enrollment payment counts, the
   customer summary, enrolling from a quote) is now composed in C# through the modules'
   *.Contracts interfaces. Foreign keys between schemas are dropped for the same reason:
   once each module has its own database, the database can't enforce them anyway.

   Run order: after 01-10 on a fresh database, or on its own against an existing one.
   Safe to re-run. Existing data is kept: tables are moved, not copied.
   Error numbers are unchanged; the ones whose rule now lives in C# are noted below. */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ===================== 1. Schemas ===================== */
IF SCHEMA_ID('accounts')    IS NULL EXEC('CREATE SCHEMA accounts');
IF SCHEMA_ID('catalog')     IS NULL EXEC('CREATE SCHEMA catalog');
IF SCHEMA_ID('quotes')      IS NULL EXEC('CREATE SCHEMA quotes');
IF SCHEMA_ID('enrollments') IS NULL EXEC('CREATE SCHEMA enrollments');
IF SCHEMA_ID('billing')     IS NULL EXEC('CREATE SCHEMA billing');
IF SCHEMA_ID('claims')      IS NULL EXEC('CREATE SCHEMA claims');
GO

/* ===================== 2. Retire the old dbo objects ===================== */
DROP VIEW IF EXISTS dbo.vw_ClaimDetails, dbo.vw_CustomerSummary, dbo.vw_EnrollmentDetails,
                    dbo.vw_PaymentDetails, dbo.vw_QuoteDetails;

DROP PROCEDURE IF EXISTS
    dbo.usp_User_Register, dbo.usp_User_GetByEmail, dbo.usp_User_GetById,
    dbo.usp_Customer_GetAll, dbo.usp_Customer_SetActive,
    dbo.usp_Policy_Create, dbo.usp_Policy_GetAll, dbo.usp_Policy_GetById, dbo.usp_Policy_Update, dbo.usp_Policy_Deactivate,
    dbo.usp_Quote_Create, dbo.usp_Quote_GetById,
    dbo.usp_Enrollment_Create, dbo.usp_Enrollment_GetByUser, dbo.usp_Enrollment_GetById, dbo.usp_Enrollment_Cancel,
    dbo.usp_Payment_GetByEnrollment, dbo.usp_Payment_GetById, dbo.usp_Payment_Pay,
    dbo.usp_Claim_GetByUser, dbo.usp_Claim_GetAll, dbo.usp_Claim_GetById, dbo.usp_Claim_Submit, dbo.usp_Claim_Review;
GO

/* ===================== 3. Cross-module foreign keys ===================== */
IF OBJECT_ID('dbo.Enrollments', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Enrollments     DROP CONSTRAINT IF EXISTS FK_Enrollments_Users, FK_Enrollments_Policies;
    ALTER TABLE dbo.PremiumPayments DROP CONSTRAINT IF EXISTS FK_Payments_Enrollments;
    ALTER TABLE dbo.Claims          DROP CONSTRAINT IF EXISTS FK_Claims_Enrollments, FK_Claims_Reviewer;
    ALTER TABLE dbo.Quotes          DROP CONSTRAINT IF EXISTS FK_Quotes_Users, FK_Quotes_Policies, FK_Quotes_Enrollments;
END
GO

/* ===================== 4. Claims carry their customer =====================
   Claims used to find their customer through Enrollments. Copy it onto the claim
   so "my claims" is answered inside the claims schema. */
IF OBJECT_ID('dbo.Claims', 'U') IS NOT NULL AND COL_LENGTH('dbo.Claims', 'UserId') IS NULL
    ALTER TABLE dbo.Claims ADD UserId INT NULL;
GO

IF OBJECT_ID('dbo.Claims', 'U') IS NOT NULL
BEGIN
    UPDATE c SET UserId = e.UserId
    FROM dbo.Claims c
    JOIN dbo.Enrollments e ON e.EnrollmentId = c.EnrollmentId
    WHERE c.UserId IS NULL;

    ALTER TABLE dbo.Claims ALTER COLUMN UserId INT NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Claims_UserId')
        CREATE INDEX IX_Claims_UserId ON dbo.Claims(UserId);
END
GO

/* ===================== 5. Move tables into their schemas ===================== */
IF OBJECT_ID('dbo.Users', 'U')           IS NOT NULL ALTER SCHEMA accounts    TRANSFER dbo.Users;
IF OBJECT_ID('dbo.Policies', 'U')        IS NOT NULL ALTER SCHEMA catalog     TRANSFER dbo.Policies;
IF OBJECT_ID('dbo.Quotes', 'U')          IS NOT NULL ALTER SCHEMA quotes      TRANSFER dbo.Quotes;
IF OBJECT_ID('dbo.Enrollments', 'U')     IS NOT NULL ALTER SCHEMA enrollments TRANSFER dbo.Enrollments;
IF OBJECT_ID('dbo.PremiumPayments', 'U') IS NOT NULL ALTER SCHEMA billing     TRANSFER dbo.PremiumPayments;
IF OBJECT_ID('dbo.Claims', 'U')          IS NOT NULL ALTER SCHEMA claims      TRANSFER dbo.Claims;
IF OBJECT_ID('dbo.ClaimNumberSeq', 'SO') IS NOT NULL ALTER SCHEMA claims      TRANSFER dbo.ClaimNumberSeq;
GO

/* =====================================================================
   ACCOUNTS (Identity module)
   ===================================================================== */

CREATE OR ALTER PROCEDURE accounts.usp_User_Register
    @FullName     NVARCHAR(100),
    @Email        NVARCHAR(256),
    @PasswordHash NVARCHAR(100),
    @Phone        NVARCHAR(20)  = NULL,
    @Address      NVARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- 50001 is our own error number (user-defined errors must be >= 50000).
    -- C# catches SqlException.Number == 50001 and turns it into a 409 Conflict.
    IF EXISTS (SELECT 1 FROM accounts.Users WHERE Email = @Email)
        THROW 50001, 'Email is already registered.', 1;

    INSERT INTO accounts.Users (FullName, Email, PasswordHash, Phone, Address)
    VALUES (@FullName, @Email, @PasswordHash, @Phone, @Address);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS UserId;
END
GO

CREATE OR ALTER PROCEDURE accounts.usp_User_GetByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, FullName, Email, PasswordHash, Phone, Address, Role, IsActive, CreatedAt
    FROM accounts.Users
    WHERE Email = @Email;
END
GO

CREATE OR ALTER PROCEDURE accounts.usp_User_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, FullName, Email, PasswordHash, Phone, Address, Role, IsActive, CreatedAt
    FROM accounts.Users
    WHERE UserId = @UserId;
END
GO

/* For other modules (claim screens show customer and reviewer names). @UserIds is a JSON array: [1,2,3]. */
CREATE OR ALTER PROCEDURE accounts.usp_User_GetMany
    @UserIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, FullName, Email, Role, IsActive
    FROM accounts.Users
    WHERE UserId IN (SELECT CAST(value AS INT) FROM OPENJSON(@UserIds));
END
GO

/* Customers only (admins never appear). The policy and claim counts are added by the API
   from the Enrollments and Claims modules. Optional filters: pass NULL to skip one. */
CREATE OR ALTER PROCEDURE accounts.usp_Customer_GetAll
    @Search   NVARCHAR(100) = NULL,
    @IsActive BIT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, FullName, Email, Phone, IsActive, CreatedAt
    FROM accounts.Users
    WHERE Role = 'Customer'
      AND (@IsActive IS NULL OR IsActive = @IsActive)
      AND (@Search IS NULL OR FullName LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')
    ORDER BY FullName;
END
GO

/* The Role = 'Customer' filter in the UPDATE is the safety net: admins can't be deactivated here.
   Returns the updated row, or no row at all if the id isn't a customer (the API turns that into a 404). */
CREATE OR ALTER PROCEDURE accounts.usp_Customer_SetActive
    @UserId   INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE accounts.Users
    SET IsActive = @IsActive
    WHERE UserId = @UserId AND Role = 'Customer';

    SELECT UserId, FullName, Email, Phone, IsActive, CreatedAt
    FROM accounts.Users
    WHERE UserId = @UserId AND Role = 'Customer';
END
GO

/* =====================================================================
   CATALOG
   ===================================================================== */

CREATE OR ALTER PROCEDURE catalog.usp_Policy_Create
    @PolicyName     NVARCHAR(100),
    @PolicyType     NVARCHAR(30),
    @Description    NVARCHAR(1000) = NULL,
    @CoverageAmount DECIMAL(18,2),
    @MonthlyPremium DECIMAL(18,2),
    @TermMonths     INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO catalog.Policies (PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths)
    VALUES (@PolicyName, @PolicyType, @Description, @CoverageAmount, @MonthlyPremium, @TermMonths);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS PolicyId;
END
GO

CREATE OR ALTER PROCEDURE catalog.usp_Policy_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PolicyId, PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths, IsActive, CreatedAt
    FROM catalog.Policies
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY PolicyName;
END
GO

CREATE OR ALTER PROCEDURE catalog.usp_Policy_GetById
    @PolicyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PolicyId, PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths, IsActive, CreatedAt
    FROM catalog.Policies
    WHERE PolicyId = @PolicyId;
END
GO

/* For other modules: every plan in the list, active or not. @PolicyIds is a JSON array. */
CREATE OR ALTER PROCEDURE catalog.usp_Policy_GetMany
    @PolicyIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PolicyId, PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths, IsActive, CreatedAt
    FROM catalog.Policies
    WHERE PolicyId IN (SELECT CAST(value AS INT) FROM OPENJSON(@PolicyIds));
END
GO

-- NOTE: NOCOUNT is deliberately NOT set ON in Update/Deactivate so that
-- ExecuteNonQueryAsync() returns the rows-affected count (0 => "not found" => 404).
CREATE OR ALTER PROCEDURE catalog.usp_Policy_Update
    @PolicyId       INT,
    @PolicyName     NVARCHAR(100),
    @PolicyType     NVARCHAR(30),
    @Description    NVARCHAR(1000) = NULL,
    @CoverageAmount DECIMAL(18,2),
    @MonthlyPremium DECIMAL(18,2),
    @TermMonths     INT
AS
BEGIN
    UPDATE catalog.Policies
    SET PolicyName = @PolicyName, PolicyType = @PolicyType, Description = @Description,
        CoverageAmount = @CoverageAmount, MonthlyPremium = @MonthlyPremium, TermMonths = @TermMonths
    WHERE PolicyId = @PolicyId;
END
GO

-- "Delete" = soft delete: existing enrollments and quotes still refer to the plan.
CREATE OR ALTER PROCEDURE catalog.usp_Policy_Deactivate
    @PolicyId INT
AS
BEGIN
    UPDATE catalog.Policies SET IsActive = 0 WHERE PolicyId = @PolicyId;
END
GO

/* =====================================================================
   QUOTES
   The API checks that the plan is active before pricing (it reads the plan anyway).
   ===================================================================== */

/* Only our API calls this, and it computes the premium. Customers can never send a price. */
CREATE OR ALTER PROCEDURE quotes.usp_Quote_Create
    @UserId         INT,
    @PolicyId       INT,
    @Answers        NVARCHAR(MAX),
    @Breakdown      NVARCHAR(MAX),
    @BasePremium    DECIMAL(18,2),
    @MonthlyPremium DECIMAL(18,2),
    @ExpiresAt      DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO quotes.Quotes (UserId, PolicyId, Answers, Breakdown, BasePremium, MonthlyPremium, ExpiresAt)
    VALUES (@UserId, @PolicyId, @Answers, @Breakdown, @BasePremium, @MonthlyPremium, @ExpiresAt);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS QuoteId;
END
GO

CREATE OR ALTER PROCEDURE quotes.usp_Quote_GetById
    @QuoteId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT QuoteId, UserId, PolicyId, BasePremium, MonthlyPremium, Breakdown, CreatedAt, ExpiresAt, EnrollmentId
    FROM quotes.Quotes
    WHERE QuoteId = @QuoteId;
END
GO

/* Last step of enrolling: mark the quote used so it can't buy a second policy.
   Returns 1, or 0 if another request used it first (the API then undoes the enrollment). */
CREATE OR ALTER PROCEDURE quotes.usp_Quote_MarkUsed
    @QuoteId      INT,
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE quotes.Quotes
    SET EnrollmentId = @EnrollmentId
    WHERE QuoteId = @QuoteId AND EnrollmentId IS NULL;

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

/* =====================================================================
   ENROLLMENTS
   Payment counts and alerts come from the Billing module; plan names from Catalog.
   ===================================================================== */

/* Nothing flips Status to 'Expired' automatically, so we derive it when reading. */
CREATE OR ALTER VIEW enrollments.vw_EnrollmentDetails
AS
SELECT  EnrollmentId, UserId, PolicyId, StartDate, EndDate, MonthlyPremium,
        CASE WHEN Status = 'Active' AND EndDate < CAST(SYSUTCDATETIME() AS DATE)
             THEN 'Expired' ELSE Status END AS Status,
        CreatedAt
FROM enrollments.Enrollments;
GO

/* The quote, the plan and the premium schedule are handled by the API, one module at a time.
   This proc owns the one rule that needs a lock: one active enrollment per plan per customer.
   Error numbers: 50002 duplicate active enrollment
   (50003 plan unavailable and 50016-50018 quote problems are now raised by the API.) */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_Create
    @UserId         INT,
    @PolicyId       INT,
    @StartDate      DATE,
    @TermMonths     INT,
    @MonthlyPremium DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EnrollmentId INT;

    BEGIN TRANSACTION;

        -- UPDLOCK + HOLDLOCK: two simultaneous requests can't both pass this check.
        IF EXISTS (SELECT 1 FROM enrollments.Enrollments WITH (UPDLOCK, HOLDLOCK)
                   WHERE UserId = @UserId AND PolicyId = @PolicyId
                     AND Status = 'Active' AND EndDate >= CAST(SYSUTCDATETIME() AS DATE))
            THROW 50002, 'You already have an active enrollment in this policy.', 1;

        INSERT INTO enrollments.Enrollments (UserId, PolicyId, StartDate, EndDate, MonthlyPremium)
        VALUES (@UserId, @PolicyId, @StartDate, DATEADD(MONTH, @TermMonths, @StartDate), @MonthlyPremium);

        SET @EnrollmentId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT @EnrollmentId AS EnrollmentId;
END
GO

/* Compensation only: removes an enrollment whose later enrollment steps failed. */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_Delete
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM enrollments.Enrollments WHERE EnrollmentId = @EnrollmentId;
END
GO

CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM enrollments.vw_EnrollmentDetails
    WHERE UserId = @UserId
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_GetById
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM enrollments.vw_EnrollmentDetails
    WHERE EnrollmentId = @EnrollmentId;
END
GO

/* For other modules. @EnrollmentIds is a JSON array. */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_GetMany
    @EnrollmentIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM enrollments.vw_EnrollmentDetails
    WHERE EnrollmentId IN (SELECT CAST(value AS INT) FROM OPENJSON(@EnrollmentIds));
END
GO

/* For the admin customer list. Customers with none are simply missing from the result. */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_CountActiveByUser
    @UserIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, COUNT(*) AS ActivePolicies
    FROM enrollments.Enrollments
    WHERE UserId IN (SELECT CAST(value AS INT) FROM OPENJSON(@UserIds))
      AND Status = 'Active' AND EndDate >= CAST(SYSUTCDATETIME() AS DATE)
    GROUP BY UserId;
END
GO

/* Cancel: only the owner, only while Active. The API then asks Billing to remove the unpaid installments. */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_Cancel
    @EnrollmentId INT,
    @UserId       INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE enrollments.Enrollments
    SET Status = 'Cancelled'
    WHERE EnrollmentId = @EnrollmentId AND UserId = @UserId AND Status = 'Active';

    SELECT @@ROWCOUNT AS RowsUpdated;
END
GO

/* =====================================================================
   BILLING
   Ownership and "is the enrollment active" are checked by the API through the Enrollments module.
   ===================================================================== */

/* Overdue is derived on read, so no job ever has to update it. An installment is Overdue only once
   it is MORE than 5 days past its due date. To change the grace period, change the 5 here and nowhere else. */
CREATE OR ALTER VIEW billing.vw_PaymentDetails
AS
SELECT  PaymentId, EnrollmentId, DueDate, Amount, PaidDate,
        CASE WHEN Status = 'Pending' AND DueDate < DATEADD(DAY, -5, CAST(SYSUTCDATETIME() AS DATE))
             THEN 'Overdue' ELSE Status END AS Status
FROM billing.PremiumPayments;
GO

/* One premium row per month. Counting 0..Term-1 and adding N months to the START date
   (not to the previous due date) keeps end-of-month starts correct: Jan 31, Feb 28, Mar 31... */
CREATE OR ALTER PROCEDURE billing.usp_Premium_CreateSchedule
    @EnrollmentId   INT,
    @StartDate      DATE,
    @TermMonths     INT,
    @MonthlyPremium DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    WITH Months AS (
        SELECT 0 AS N
        UNION ALL
        SELECT N + 1 FROM Months WHERE N + 1 < @TermMonths
    )
    INSERT INTO billing.PremiumPayments (EnrollmentId, DueDate, Amount)
    SELECT @EnrollmentId, DATEADD(MONTH, N, @StartDate), @MonthlyPremium
    FROM Months
    OPTION (MAXRECURSION 600);      -- default limit is 100; our max term is 600 months
END
GO

/* Cancelling a policy (or undoing a failed enrollment): unpaid installments go, paid ones stay as history. */
CREATE OR ALTER PROCEDURE billing.usp_Premium_RemoveUnpaid
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM billing.PremiumPayments
    WHERE EnrollmentId = @EnrollmentId AND PaidDate IS NULL;
END
GO

/* Per-enrollment totals for the policy cards and the claim rules. Overdue comes from the view above,
   the single source of truth (including the grace period). @EnrollmentIds is a JSON array. */
CREATE OR ALTER PROCEDURE billing.usp_Premium_GetSummaries
    @EnrollmentIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT  ids.EnrollmentId,
            (SELECT COUNT(*) FROM billing.PremiumPayments pp WHERE pp.EnrollmentId = ids.EnrollmentId AND pp.Status = 'Paid') AS PaymentsPaid,
            (SELECT COUNT(*) FROM billing.PremiumPayments pp WHERE pp.EnrollmentId = ids.EnrollmentId)                       AS PaymentsTotal,
            (SELECT COUNT(*) FROM billing.vw_PaymentDetails pd WHERE pd.EnrollmentId = ids.EnrollmentId AND pd.Status = 'Overdue') AS OverdueCount,
            nxt.DueDate AS NextDueDate,        -- earliest unpaid installment (NULL if none left)
            nxt.Amount  AS NextDueAmount
    FROM (SELECT DISTINCT CAST(value AS INT) AS EnrollmentId FROM OPENJSON(@EnrollmentIds)) ids
    OUTER APPLY (
        SELECT TOP (1) pp.DueDate, pp.Amount
        FROM billing.PremiumPayments pp
        WHERE pp.EnrollmentId = ids.EnrollmentId AND pp.PaidDate IS NULL
        ORDER BY pp.DueDate
    ) nxt;
END
GO

CREATE OR ALTER PROCEDURE billing.usp_Payment_GetByEnrollment
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM billing.vw_PaymentDetails
    WHERE EnrollmentId = @EnrollmentId
    ORDER BY DueDate;
END
GO

CREATE OR ALTER PROCEDURE billing.usp_Payment_GetById
    @PaymentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM billing.vw_PaymentDetails
    WHERE PaymentId = @PaymentId;
END
GO

/* Pay one installment. The checks and the update happen in one transaction, with the row locked,
   so two simultaneous "Pay" clicks can't both succeed.

   Error numbers used by this proc:
     50004  payment not found
     50005  already paid
     50007  an earlier installment is still unpaid
   (Ownership, also reported as 50004's "not found", and 50006 "enrollment not active" are checked by the API.) */
CREATE OR ALTER PROCEDURE billing.usp_Payment_Pay
    @PaymentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EnrollmentId INT, @DueDate DATE, @PaidDate DATE;

    BEGIN TRANSACTION;

        SELECT @EnrollmentId = EnrollmentId, @DueDate = DueDate, @PaidDate = PaidDate
        FROM billing.PremiumPayments WITH (UPDLOCK)
        WHERE PaymentId = @PaymentId;

        IF @EnrollmentId IS NULL
            THROW 50004, 'Payment not found.', 1;

        IF @PaidDate IS NOT NULL
            THROW 50005, 'This installment has already been paid.', 1;

        IF EXISTS (SELECT 1 FROM billing.PremiumPayments
                   WHERE EnrollmentId = @EnrollmentId AND PaidDate IS NULL AND DueDate < @DueDate)
            THROW 50007, 'Please pay your earlier installments first.', 1;

        UPDATE billing.PremiumPayments
        SET Status = 'Paid', PaidDate = CAST(SYSUTCDATETIME() AS DATE)
        WHERE PaymentId = @PaymentId;

    COMMIT TRANSACTION;

    SELECT * FROM billing.vw_PaymentDetails WHERE PaymentId = @PaymentId;   -- return the updated row
END
GO

/* =====================================================================
   CLAIMS
   Customer and reviewer names come from Identity, plan names and coverage from Catalog,
   enrollment state from Enrollments, overdue premiums from Billing.
   ===================================================================== */

CREATE OR ALTER VIEW claims.vw_ClaimDetails
AS
SELECT  ClaimId, ClaimNumber, EnrollmentId, UserId,
        IncidentDate, Description, ClaimAmount, Status, AdminNotes,
        SubmittedAt, ReviewedAt, ReviewedByUserId
FROM claims.Claims;
GO

CREATE OR ALTER PROCEDURE claims.usp_Claim_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM claims.vw_ClaimDetails
    WHERE UserId = @UserId
    ORDER BY SubmittedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE claims.usp_Claim_GetAll
    @Status NVARCHAR(20) = NULL      -- NULL = every status
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM claims.vw_ClaimDetails
    WHERE @Status IS NULL OR Status = @Status
    ORDER BY SubmittedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE claims.usp_Claim_GetById
    @ClaimId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM claims.vw_ClaimDetails WHERE ClaimId = @ClaimId;
END
GO

/* For the admin customer list. Customers with no claims are simply missing from the result. */
CREATE OR ALTER PROCEDURE claims.usp_Claim_CountByUser
    @UserIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId,
           COUNT(*) AS TotalClaims,
           SUM(CASE WHEN Status IN ('Submitted', 'UnderReview') THEN 1 ELSE 0 END) AS OpenClaims
    FROM claims.Claims
    WHERE UserId IN (SELECT CAST(value AS INT) FROM OPENJSON(@UserIds))
    GROUP BY UserId;
END
GO

/* Submit a claim. The API has already checked the enrollment (owner, active, dates, overdue premiums)
   and looked up @CoverageAmount. This proc owns the rule that needs a lock: remaining coverage.
   Error numbers:
     50012 amount exceeds remaining coverage
   (50008 not found/not yours, 50009 not active, 50010 bad incident date, 50011 overdue premiums
    are now raised by the API.) */
CREATE OR ALTER PROCEDURE claims.usp_Claim_Submit
    @UserId         INT,
    @EnrollmentId   INT,
    @IncidentDate   DATE,
    @Description    NVARCHAR(2000),
    @ClaimAmount    DECIMAL(18,2),
    @CoverageAmount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Today DATE = CAST(SYSUTCDATETIME() AS DATE);
    DECLARE @Used DECIMAL(18,2), @Seq BIGINT, @ClaimNumber NVARCHAR(20), @ClaimId INT, @Msg NVARCHAR(200);

    BEGIN TRANSACTION;

        -- Lock this enrollment's claims so a concurrent approval can't change "used" under us.
        SELECT @Used = ISNULL(SUM(ClaimAmount), 0)
        FROM claims.Claims WITH (UPDLOCK, HOLDLOCK)
        WHERE EnrollmentId = @EnrollmentId AND Status IN ('Approved', 'Paid');

        IF @ClaimAmount > @CoverageAmount - @Used
        BEGIN
            SET @Msg = CONCAT('The claim amount exceeds your remaining coverage of ',
                              FORMAT(@CoverageAmount - @Used, 'C', 'en-US'), '.');
            THROW 50012, @Msg, 1;
        END

        SET @Seq = NEXT VALUE FOR claims.ClaimNumberSeq;
        SET @ClaimNumber = CONCAT('CLM-', YEAR(@Today), '-', RIGHT(CONCAT('000000', @Seq), 6));

        INSERT INTO claims.Claims (ClaimNumber, EnrollmentId, UserId, IncidentDate, Description, ClaimAmount)
        VALUES (@ClaimNumber, @EnrollmentId, @UserId, @IncidentDate, @Description, @ClaimAmount);

        SET @ClaimId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT @ClaimId AS ClaimId;
END
GO

/* Move a claim to its next status. The API has already checked that the move is allowed;
   @FromStatus is the status it read, so a concurrent change is detected (optimistic concurrency).
   @CoverageAmount is required when approving.
   Error numbers:
     50013 claim changed by someone else (stale @FromStatus)
     50014 claim not found
     50015 approving would exceed remaining coverage
     50019 approving without @CoverageAmount (a bug in the caller, never a user error) */
CREATE OR ALTER PROCEDURE claims.usp_Claim_Review
    @ClaimId        INT,
    @FromStatus     NVARCHAR(20),
    @ToStatus       NVARCHAR(20),
    @AdminNotes     NVARCHAR(1000) = NULL,
    @ReviewerId     INT,
    @CoverageAmount DECIMAL(18,2)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EnrollmentId INT, @Amount DECIMAL(18,2), @Used DECIMAL(18,2);

    BEGIN TRANSACTION;

        SELECT @EnrollmentId = EnrollmentId, @Amount = ClaimAmount
        FROM claims.Claims WITH (UPDLOCK)
        WHERE ClaimId = @ClaimId;

        IF @EnrollmentId IS NULL
            THROW 50014, 'Claim not found.', 1;

        UPDATE claims.Claims
        SET Status           = @ToStatus,
            AdminNotes       = COALESCE(@AdminNotes, AdminNotes),   -- keep earlier notes if none supplied
            ReviewedAt       = SYSUTCDATETIME(),
            ReviewedByUserId = @ReviewerId
        WHERE ClaimId = @ClaimId AND Status = @FromStatus;

        IF @@ROWCOUNT = 0
            THROW 50013, 'This claim was changed by someone else. Please refresh and try again.', 1;

        IF @ToStatus = 'Approved'
        BEGIN
            IF @CoverageAmount IS NULL
                THROW 50019, '@CoverageAmount is required when approving a claim.', 1;

            -- Lock the enrollment's claims so two approvals for the same policy can't both pass this check.
            SELECT @Used = ISNULL(SUM(ClaimAmount), 0)
            FROM claims.Claims WITH (UPDLOCK, HOLDLOCK)
            WHERE EnrollmentId = @EnrollmentId AND Status IN ('Approved', 'Paid') AND ClaimId <> @ClaimId;

            IF @Amount > @CoverageAmount - @Used
                THROW 50015, 'Approving this claim would exceed the remaining coverage on this policy.', 1;
        END

    COMMIT TRANSACTION;

    SELECT * FROM claims.vw_ClaimDetails WHERE ClaimId = @ClaimId;
END
GO

PRINT 'Module schemas are in place.';
GO
