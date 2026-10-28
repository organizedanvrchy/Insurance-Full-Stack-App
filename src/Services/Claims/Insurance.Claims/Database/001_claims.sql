/* Claims service database (InsuranceClaims). Run automatically by the service at startup
   (DatabaseMigrator): each script runs once, in name order, and is recorded in dbo.SchemaVersions.
   Only the Claims service reads or writes this database. */

CREATE SCHEMA claims;
GO

/* Claim numbers come from a SEQUENCE: a counter owned by the database. Rolled-back inserts can leave gaps; that's normal. */
CREATE SEQUENCE claims.ClaimNumberSeq AS BIGINT START WITH 1 INCREMENT BY 1;

-- EnrollmentId, UserId and ReviewedByUserId belong to other services: plain ids, no foreign keys.
CREATE TABLE claims.Claims (
    ClaimId          INT IDENTITY(1,1) CONSTRAINT PK_Claims PRIMARY KEY,
    ClaimNumber      NVARCHAR(20)   NOT NULL,          -- human-friendly, e.g. CLM-2026-000123
    EnrollmentId     INT NOT NULL,
    UserId           INT NOT NULL,                     -- the customer
    IncidentDate     DATE NOT NULL,
    Description      NVARCHAR(2000) NOT NULL,
    ClaimAmount      DECIMAL(18,2)  NOT NULL,
    Status           NVARCHAR(20)   NOT NULL CONSTRAINT DF_Claims_Status DEFAULT 'Submitted',
    AdminNotes       NVARCHAR(1000) NULL,
    SubmittedAt      DATETIME2 NOT NULL CONSTRAINT DF_Claims_SubmittedAt DEFAULT SYSUTCDATETIME(),
    ReviewedAt       DATETIME2 NULL,
    ReviewedByUserId INT NULL,
    CONSTRAINT UQ_Claims_Number UNIQUE (ClaimNumber),
    CONSTRAINT CK_Claims_Status CHECK (Status IN ('Submitted','UnderReview','Approved','Rejected','Paid')),
    CONSTRAINT CK_Claims_Amount CHECK (ClaimAmount > 0)
);
CREATE INDEX IX_Claims_EnrollmentId ON claims.Claims(EnrollmentId);
CREATE INDEX IX_Claims_UserId ON claims.Claims(UserId);
GO

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

/* Submit a claim. The Claims service has already checked the enrollment (owner, active, dates, overdue premiums)
   and looked up @CoverageAmount. This proc owns the rule that needs a lock: remaining coverage.
   Error numbers:
     50012 amount exceeds remaining coverage
   (50008 not found/not yours, 50009 not active, 50010 bad incident date, 50011 overdue premiums
    are raised by the Claims service, from what it reads from other services.) */
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

/* Move a claim to its next status. The Claims service has already checked that the move is allowed;
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
