CREATE TABLE dbo.Users (
    UserId       INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    FullName     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(100) NOT NULL,           -- BCrypt hash (60 chars). Never store the password.
    Phone        NVARCHAR(20)  NULL,
    Address      NVARCHAR(250) NULL,
    Role         NVARCHAR(20)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Customer',
    IsActive     BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer','Admin'))
);

CREATE TABLE dbo.Policies (
    PolicyId       INT IDENTITY(1,1) CONSTRAINT PK_Policies PRIMARY KEY,
    PolicyName     NVARCHAR(100)  NOT NULL,
    PolicyType     NVARCHAR(30)   NOT NULL,
    Description    NVARCHAR(1000) NULL,
    CoverageAmount DECIMAL(18,2)  NOT NULL,
    MonthlyPremium DECIMAL(18,2)  NOT NULL,
    TermMonths     INT            NOT NULL,
    IsActive       BIT            NOT NULL CONSTRAINT DF_Policies_IsActive DEFAULT 1,
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Policies_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Policies_Type     CHECK (PolicyType IN ('Health','Auto','Life','Home','Travel')),
    CONSTRAINT CK_Policies_Coverage CHECK (CoverageAmount > 0),
    CONSTRAINT CK_Policies_Premium  CHECK (MonthlyPremium > 0),
    CONSTRAINT CK_Policies_Term     CHECK (TermMonths > 0)
);

-- A customer's purchase of a policy. Premium is copied (snapshot) so later price changes don't rewrite history.
CREATE TABLE dbo.Enrollments (
    EnrollmentId   INT IDENTITY(1,1) CONSTRAINT PK_Enrollments PRIMARY KEY,
    UserId         INT NOT NULL CONSTRAINT FK_Enrollments_Users    FOREIGN KEY REFERENCES dbo.Users(UserId),
    PolicyId       INT NOT NULL CONSTRAINT FK_Enrollments_Policies FOREIGN KEY REFERENCES dbo.Policies(PolicyId),
    StartDate      DATE NOT NULL,
    EndDate        DATE NOT NULL,
    MonthlyPremium DECIMAL(18,2) NOT NULL,
    Status         NVARCHAR(20) NOT NULL CONSTRAINT DF_Enrollments_Status DEFAULT 'Active',
    CreatedAt      DATETIME2 NOT NULL CONSTRAINT DF_Enrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Enrollments_Status CHECK (Status IN ('Active','Cancelled','Expired')),
    CONSTRAINT CK_Enrollments_Dates  CHECK (EndDate > StartDate)
);
CREATE INDEX IX_Enrollments_UserId ON dbo.Enrollments(UserId);

-- One row per monthly installment. Generate these when someone enrolls.
CREATE TABLE dbo.PremiumPayments (
    PaymentId    INT IDENTITY(1,1) CONSTRAINT PK_PremiumPayments PRIMARY KEY,
    EnrollmentId INT NOT NULL CONSTRAINT FK_Payments_Enrollments FOREIGN KEY REFERENCES dbo.Enrollments(EnrollmentId),
    DueDate      DATE NOT NULL,
    Amount       DECIMAL(18,2) NOT NULL,
    PaidDate     DATE NULL,
    Status       NVARCHAR(20) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'Pending',
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('Pending','Paid','Overdue'))
);
CREATE INDEX IX_Payments_EnrollmentId ON dbo.PremiumPayments(EnrollmentId);

CREATE TABLE dbo.Claims (
    ClaimId          INT IDENTITY(1,1) CONSTRAINT PK_Claims PRIMARY KEY,
    ClaimNumber      NVARCHAR(20)   NOT NULL,          -- human-friendly, e.g. CLM-2026-000123
    EnrollmentId     INT NOT NULL CONSTRAINT FK_Claims_Enrollments FOREIGN KEY REFERENCES dbo.Enrollments(EnrollmentId),
    IncidentDate     DATE NOT NULL,
    Description      NVARCHAR(2000) NOT NULL,
    ClaimAmount      DECIMAL(18,2)  NOT NULL,
    Status           NVARCHAR(20)   NOT NULL CONSTRAINT DF_Claims_Status DEFAULT 'Submitted',
    AdminNotes       NVARCHAR(1000) NULL,
    SubmittedAt      DATETIME2 NOT NULL CONSTRAINT DF_Claims_SubmittedAt DEFAULT SYSUTCDATETIME(),
    ReviewedAt       DATETIME2 NULL,
    ReviewedByUserId INT NULL CONSTRAINT FK_Claims_Reviewer FOREIGN KEY REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_Claims_Number UNIQUE (ClaimNumber),
    CONSTRAINT CK_Claims_Status CHECK (Status IN ('Submitted','UnderReview','Approved','Rejected','Paid')),
    CONSTRAINT CK_Claims_Amount CHECK (ClaimAmount > 0)
);
CREATE INDEX IX_Claims_EnrollmentId ON dbo.Claims(EnrollmentId);
GO
