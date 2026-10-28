/* Catalog service database (InsuranceCatalog). Run automatically by the service at startup
   (DatabaseMigrator): each script runs once, in name order, and is recorded in dbo.SchemaVersions.
   Only the Catalog service reads or writes this database. */

CREATE SCHEMA catalog;
GO

CREATE TABLE catalog.Policies (
    PolicyId       INT IDENTITY(1,1) CONSTRAINT PK_Policies PRIMARY KEY,
    PolicyName     NVARCHAR(100)  NOT NULL,
    PolicyType     NVARCHAR(30)   NOT NULL,
    Description    NVARCHAR(1000) NULL,
    CoverageAmount DECIMAL(18,2)  NOT NULL,
    MonthlyPremium DECIMAL(18,2)  NOT NULL,       -- the BASE monthly rate; quotes multiply it by rating factors
    TermMonths     INT            NOT NULL,
    IsActive       BIT            NOT NULL CONSTRAINT DF_Policies_IsActive DEFAULT 1,
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Policies_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Policies_Type     CHECK (PolicyType IN ('Health','Auto','Life','Home','Travel')),
    CONSTRAINT CK_Policies_Coverage CHECK (CoverageAmount > 0),
    CONSTRAINT CK_Policies_Premium  CHECK (MonthlyPremium > 0),
    CONSTRAINT CK_Policies_Term     CHECK (TermMonths > 0)
);
GO

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

/* For other services: every plan in the list, active or not. @PolicyIds is a JSON array. */
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
