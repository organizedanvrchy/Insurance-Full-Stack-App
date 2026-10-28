/* Enrollments service database (InsuranceEnrollments). Run automatically by the service at startup
   (DatabaseMigrator): each script runs once, in name order, and is recorded in dbo.SchemaVersions.
   Only the Enrollments service reads or writes this database. */

CREATE SCHEMA enrollments;
GO

-- A customer's purchase of a policy. Premium is copied from the quote so later price changes don't rewrite history.
-- Status doubles as the enrollment saga's state: Pending until Billing has created the premium schedule.
CREATE TABLE enrollments.Enrollments (
    EnrollmentId   INT IDENTITY(1,1) CONSTRAINT PK_Enrollments PRIMARY KEY,
    UserId         INT NOT NULL,
    PolicyId       INT NOT NULL,
    QuoteId        INT NOT NULL,
    StartDate      DATE NOT NULL,
    EndDate        DATE NOT NULL,
    MonthlyPremium DECIMAL(18,2) NOT NULL,
    Status         NVARCHAR(20) NOT NULL CONSTRAINT DF_Enrollments_Status DEFAULT 'Pending',
    CreatedAt      DATETIME2 NOT NULL CONSTRAINT DF_Enrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Enrollments_Status CHECK (Status IN ('Pending','Active','Cancelled','Expired','Failed')),
    CONSTRAINT CK_Enrollments_Dates  CHECK (EndDate > StartDate)
);
CREATE INDEX IX_Enrollments_UserId ON enrollments.Enrollments(UserId);

-- Transactional outbox: events are written here in the same transaction as the change they describe,
-- then published to the message bus by a background dispatcher, so an event is never lost or sent for a rolled-back change.
CREATE TABLE dbo.OutboxMessages (
    Id          BIGINT IDENTITY(1,1) CONSTRAINT PK_OutboxMessages PRIMARY KEY,
    MessageType NVARCHAR(300) NOT NULL,
    Payload     NVARCHAR(MAX) NOT NULL,
    OccurredAt  DATETIME2 NOT NULL CONSTRAINT DF_OutboxMessages_OccurredAt DEFAULT SYSUTCDATETIME(),
    PublishedAt DATETIME2 NULL
);
CREATE INDEX IX_OutboxMessages_Unpublished ON dbo.OutboxMessages(Id) WHERE PublishedAt IS NULL;
GO

/* Nothing flips Status to 'Expired' automatically, so we derive it when reading. */
CREATE OR ALTER VIEW enrollments.vw_EnrollmentDetails
AS
SELECT  EnrollmentId, UserId, PolicyId, QuoteId, StartDate, EndDate, MonthlyPremium,
        CASE WHEN Status = 'Active' AND EndDate < CAST(SYSUTCDATETIME() AS DATE)
             THEN 'Expired' ELSE Status END AS Status,
        CreatedAt
FROM enrollments.Enrollments;
GO

/* Step 2 of the enrollment saga. The quote is already reserved (Quotes service) and the plan checked (Catalog service).
   Inserts the enrollment as Pending; the caller writes the EnrollmentRequested event to the outbox in the same transaction.
   Pending enrollments count as taken, so two requests can't both start enrolling in the same plan.
   Error numbers: 50002 duplicate active enrollment */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_CreatePending
    @UserId         INT,
    @PolicyId       INT,
    @QuoteId        INT,
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
                     AND Status IN ('Active', 'Pending') AND EndDate >= CAST(SYSUTCDATETIME() AS DATE))
            THROW 50002, 'You already have an active enrollment in this policy.', 1;

        INSERT INTO enrollments.Enrollments (UserId, PolicyId, QuoteId, StartDate, EndDate, MonthlyPremium, Status)
        VALUES (@UserId, @PolicyId, @QuoteId, @StartDate, DATEADD(MONTH, @TermMonths, @StartDate), @MonthlyPremium, 'Pending');

        SET @EnrollmentId = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT @EnrollmentId AS EnrollmentId;
END
GO

/* Saga step 3 succeeded (Billing created the schedule) or failed. Pending -> Active or Failed.
   Returns the row either way, so a redelivered message finds the saga already finished and does nothing new. */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_Complete
    @EnrollmentId INT,
    @Succeeded    BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE enrollments.Enrollments
    SET Status = CASE WHEN @Succeeded = 1 THEN 'Active' ELSE 'Failed' END
    WHERE EnrollmentId = @EnrollmentId AND Status = 'Pending';

    SELECT * FROM enrollments.Enrollments WHERE EnrollmentId = @EnrollmentId;
END
GO

CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_GetByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM enrollments.vw_EnrollmentDetails
    WHERE UserId = @UserId AND Status NOT IN ('Pending', 'Failed')
    ORDER BY CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_GetById
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM enrollments.vw_EnrollmentDetails
    WHERE EnrollmentId = @EnrollmentId AND Status <> 'Failed';
END
GO

/* For other services. @EnrollmentIds is a JSON array. */
CREATE OR ALTER PROCEDURE enrollments.usp_Enrollment_GetMany
    @EnrollmentIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM enrollments.vw_EnrollmentDetails
    WHERE EnrollmentId IN (SELECT CAST(value AS INT) FROM OPENJSON(@EnrollmentIds)) AND Status <> 'Failed';
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

/* Cancel: only the owner, only while Active. The caller writes EnrollmentCancelled to the outbox in the same
   transaction; Billing removes the unpaid installments when it receives it. */
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
