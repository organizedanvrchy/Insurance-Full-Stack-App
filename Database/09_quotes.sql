IF OBJECT_ID('dbo.Quotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quotes (
        QuoteId        INT IDENTITY(1,1) CONSTRAINT PK_Quotes PRIMARY KEY,
        UserId         INT NOT NULL CONSTRAINT FK_Quotes_Users    FOREIGN KEY REFERENCES dbo.Users(UserId),
        PolicyId       INT NOT NULL CONSTRAINT FK_Quotes_Policies FOREIGN KEY REFERENCES dbo.Policies(PolicyId),
        Answers        NVARCHAR(MAX) NOT NULL,                 -- JSON: what the customer told us (audit trail)
        Breakdown      NVARCHAR(MAX) NOT NULL,                 -- JSON: the rating factors applied
        BasePremium    DECIMAL(18,2) NOT NULL,
        MonthlyPremium DECIMAL(18,2) NOT NULL,                 -- the quoted price
        CreatedAt      DATETIME2 NOT NULL CONSTRAINT DF_Quotes_CreatedAt DEFAULT SYSUTCDATETIME(),
        ExpiresAt      DATETIME2 NOT NULL,
        EnrollmentId   INT NULL CONSTRAINT FK_Quotes_Enrollments FOREIGN KEY REFERENCES dbo.Enrollments(EnrollmentId),  -- set once used
        CONSTRAINT CK_Quotes_Premium CHECK (BasePremium > 0 AND MonthlyPremium > 0),
        CONSTRAINT CK_Quotes_Json    CHECK (ISJSON(Answers) = 1 AND ISJSON(Breakdown) = 1)
    );
    CREATE INDEX IX_Quotes_UserId ON dbo.Quotes(UserId);
END
GO

CREATE OR ALTER VIEW dbo.vw_QuoteDetails
AS
SELECT  q.QuoteId, q.UserId, q.PolicyId,
        p.PolicyName, p.PolicyType, p.CoverageAmount, p.TermMonths,
        q.BasePremium, q.MonthlyPremium, q.Breakdown,
        q.CreatedAt, q.ExpiresAt, q.EnrollmentId
FROM dbo.Quotes q
JOIN dbo.Policies p ON p.PolicyId = q.PolicyId;
GO

/* Only our API calls this, and it computes the premium. Customers can never send a price. */
CREATE OR ALTER PROCEDURE dbo.usp_Quote_Create
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

    IF NOT EXISTS (SELECT 1 FROM dbo.Policies WHERE PolicyId = @PolicyId AND IsActive = 1)
        THROW 50003, 'That policy does not exist or is no longer available.', 1;

    INSERT INTO dbo.Quotes (UserId, PolicyId, Answers, Breakdown, BasePremium, MonthlyPremium, ExpiresAt)
    VALUES (@UserId, @PolicyId, @Answers, @Breakdown, @BasePremium, @MonthlyPremium, @ExpiresAt);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS QuoteId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Quote_GetById
    @QuoteId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_QuoteDetails WHERE QuoteId = @QuoteId;
END
GO

/* REPLACES the old version (same name, new parameters): enrollment now comes from a quote.
   The premium and the policy come from the quote, so the caller can't choose either.
   Error numbers:
     50002 duplicate active enrollment   50003 policy unavailable
     50016 quote not found OR not yours  50017 quote expired   50018 quote already used */
CREATE OR ALTER PROCEDURE dbo.usp_Enrollment_Create
    @UserId    INT,
    @QuoteId   INT,
    @StartDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PolicyId INT, @Premium DECIMAL(18,2), @QuoteOwner INT, @Expires DATETIME2, @UsedBy INT;
    DECLARE @Term INT, @EnrollmentId INT;

    BEGIN TRANSACTION;

        SELECT @PolicyId = q.PolicyId, @Premium = q.MonthlyPremium, @QuoteOwner = q.UserId,
               @Expires = q.ExpiresAt, @UsedBy = q.EnrollmentId
        FROM dbo.Quotes q WITH (UPDLOCK)
        WHERE q.QuoteId = @QuoteId;

        IF @PolicyId IS NULL OR @QuoteOwner <> @UserId
            THROW 50016, 'Quote not found.', 1;

        IF @UsedBy IS NOT NULL
            THROW 50018, 'This quote has already been used to enroll.', 1;

        IF @Expires < SYSUTCDATETIME()
            THROW 50017, 'This quote has expired. Please request a new quote.', 1;

        SELECT @Term = TermMonths FROM dbo.Policies WHERE PolicyId = @PolicyId AND IsActive = 1;

        IF @Term IS NULL
            THROW 50003, 'That policy does not exist or is no longer available.', 1;

        IF EXISTS (SELECT 1 FROM dbo.Enrollments WITH (UPDLOCK, HOLDLOCK)
                   WHERE UserId = @UserId AND PolicyId = @PolicyId
                     AND Status = 'Active' AND EndDate >= CAST(SYSUTCDATETIME() AS DATE))
            THROW 50002, 'You already have an active enrollment in this policy.', 1;

        INSERT INTO dbo.Enrollments (UserId, PolicyId, StartDate, EndDate, MonthlyPremium)
        VALUES (@UserId, @PolicyId, @StartDate, DATEADD(MONTH, @Term, @StartDate), @Premium);

        SET @EnrollmentId = SCOPE_IDENTITY();

        WITH Months AS (
            SELECT 0 AS N
            UNION ALL
            SELECT N + 1 FROM Months WHERE N + 1 < @Term
        )
        INSERT INTO dbo.PremiumPayments (EnrollmentId, DueDate, Amount)
        SELECT @EnrollmentId, DATEADD(MONTH, N, @StartDate), @Premium
        FROM Months
        OPTION (MAXRECURSION 600);

        -- Mark the quote as used so it can't buy a second policy.
        UPDATE dbo.Quotes SET EnrollmentId = @EnrollmentId WHERE QuoteId = @QuoteId;

    COMMIT TRANSACTION;

    SELECT @EnrollmentId AS EnrollmentId;
END
GO