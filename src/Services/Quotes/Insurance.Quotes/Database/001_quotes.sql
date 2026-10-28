/* Quotes service database (InsuranceQuotes). Run automatically by the service at startup
   (DatabaseMigrator): each script runs once, in name order, and is recorded in dbo.SchemaVersions.
   Only the Quotes service reads or writes this database. */

CREATE SCHEMA quotes;
GO

-- UserId and PolicyId belong to the Identity and Catalog services: plain ids, no foreign keys.
CREATE TABLE quotes.Quotes (
    QuoteId        INT IDENTITY(1,1) CONSTRAINT PK_Quotes PRIMARY KEY,
    UserId         INT NOT NULL,
    PolicyId       INT NOT NULL,
    Answers        NVARCHAR(MAX) NOT NULL,                 -- JSON: what the customer told us (audit trail)
    Breakdown      NVARCHAR(MAX) NOT NULL,                 -- JSON: the rating factors applied
    BasePremium    DECIMAL(18,2) NOT NULL,
    MonthlyPremium DECIMAL(18,2) NOT NULL,                 -- the quoted price
    CreatedAt      DATETIME2 NOT NULL CONSTRAINT DF_Quotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    ExpiresAt      DATETIME2 NOT NULL,
    ReservedUntil  DATETIME2 NULL,                         -- held by an enrollment in progress
    EnrollmentId   INT NULL,                               -- set once the enrollment is active
    CONSTRAINT CK_Quotes_Premium CHECK (BasePremium > 0 AND MonthlyPremium > 0),
    CONSTRAINT CK_Quotes_Json    CHECK (ISJSON(Answers) = 1 AND ISJSON(Breakdown) = 1)
);
CREATE INDEX IX_Quotes_UserId ON quotes.Quotes(UserId);
GO

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
    SELECT QuoteId, UserId, PolicyId, BasePremium, MonthlyPremium, Breakdown, CreatedAt, ExpiresAt, ReservedUntil, EnrollmentId
    FROM quotes.Quotes
    WHERE QuoteId = @QuoteId;
END
GO

/* Step 1 of the enrollment saga (called by the Enrollments service): hold the quote for this customer.
   A reservation lapses after @ReserveMinutes, so a crashed enrollment can't lock a quote forever.
   Error numbers:
     50016 quote not found OR not yours
     50017 quote expired
     50018 quote already used (or held by an enrollment in progress) */
CREATE OR ALTER PROCEDURE quotes.usp_Quote_Reserve
    @QuoteId        INT,
    @UserId         INT,
    @ReserveMinutes INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Owner INT, @Expires DATETIME2, @UsedBy INT, @ReservedUntil DATETIME2, @Now DATETIME2 = SYSUTCDATETIME();

    BEGIN TRANSACTION;

        SELECT @Owner = UserId, @Expires = ExpiresAt, @UsedBy = EnrollmentId, @ReservedUntil = ReservedUntil
        FROM quotes.Quotes WITH (UPDLOCK)
        WHERE QuoteId = @QuoteId;

        IF @Owner IS NULL OR @Owner <> @UserId
            THROW 50016, 'Quote not found.', 1;

        IF @UsedBy IS NOT NULL OR @ReservedUntil > @Now
            THROW 50018, 'This quote has already been used to enroll.', 1;

        IF @Expires < @Now
            THROW 50017, 'This quote has expired. Please request a new quote.', 1;

        UPDATE quotes.Quotes
        SET ReservedUntil = DATEADD(MINUTE, @ReserveMinutes, @Now)
        WHERE QuoteId = @QuoteId;

    COMMIT TRANSACTION;

    SELECT QuoteId, PolicyId, MonthlyPremium FROM quotes.Quotes WHERE QuoteId = @QuoteId;
END
GO

/* The enrollment didn't go ahead: the quote can be used again. Never undoes a confirmed use. */
CREATE OR ALTER PROCEDURE quotes.usp_Quote_Release
    @QuoteId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE quotes.Quotes
    SET ReservedUntil = NULL
    WHERE QuoteId = @QuoteId AND EnrollmentId IS NULL;
END
GO

/* The enrollment is active: the quote is used for good. Safe to receive twice (messages can be redelivered). */
CREATE OR ALTER PROCEDURE quotes.usp_Quote_MarkUsed
    @QuoteId      INT,
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE quotes.Quotes
    SET EnrollmentId = @EnrollmentId, ReservedUntil = NULL
    WHERE QuoteId = @QuoteId AND (EnrollmentId IS NULL OR EnrollmentId = @EnrollmentId);
END
GO
