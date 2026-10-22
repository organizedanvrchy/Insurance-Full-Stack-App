USE InsuranceDb;
GO

/* =====================  USERS (auth)  ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_User_Register
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
    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email)
        THROW 50001, 'Email is already registered.', 1;

    INSERT INTO dbo.Users (FullName, Email, PasswordHash, Phone, Address)
    VALUES (@FullName, @Email, @PasswordHash, @Phone, @Address);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS UserId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, FullName, Email, PasswordHash, Phone, Address, Role, IsActive, CreatedAt
    FROM dbo.Users
    WHERE Email = @Email;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserId, FullName, Email, PasswordHash, Phone, Address, Role, IsActive, CreatedAt
    FROM dbo.Users
    WHERE UserId = @UserId;
END
GO

/* =====================  POLICIES (full CRUD template)  ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Policy_Create
    @PolicyName     NVARCHAR(100),
    @PolicyType     NVARCHAR(30),
    @Description    NVARCHAR(1000) = NULL,
    @CoverageAmount DECIMAL(18,2),
    @MonthlyPremium DECIMAL(18,2),
    @TermMonths     INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Policies (PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths)
    VALUES (@PolicyName, @PolicyType, @Description, @CoverageAmount, @MonthlyPremium, @TermMonths);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS PolicyId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Policy_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PolicyId, PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths, IsActive, CreatedAt
    FROM dbo.Policies
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY PolicyName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Policy_GetById
    @PolicyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PolicyId, PolicyName, PolicyType, Description, CoverageAmount, MonthlyPremium, TermMonths, IsActive, CreatedAt
    FROM dbo.Policies
    WHERE PolicyId = @PolicyId;
END
GO

-- NOTE: NOCOUNT is deliberately NOT set ON in Update/Deactivate so that
-- ExecuteNonQueryAsync() returns the rows-affected count (0 => "not found" => 404).
CREATE OR ALTER PROCEDURE dbo.usp_Policy_Update
    @PolicyId       INT,
    @PolicyName     NVARCHAR(100),
    @PolicyType     NVARCHAR(30),
    @Description    NVARCHAR(1000) = NULL,
    @CoverageAmount DECIMAL(18,2),
    @MonthlyPremium DECIMAL(18,2),
    @TermMonths     INT
AS
BEGIN
    UPDATE dbo.Policies
    SET PolicyName = @PolicyName, PolicyType = @PolicyType, Description = @Description,
        CoverageAmount = @CoverageAmount, MonthlyPremium = @MonthlyPremium, TermMonths = @TermMonths
    WHERE PolicyId = @PolicyId;
END
GO

-- "Delete" = soft delete. A hard DELETE would fail (or orphan data) once enrollments reference the policy.
CREATE OR ALTER PROCEDURE dbo.usp_Policy_Deactivate
    @PolicyId INT
AS
BEGIN
    UPDATE dbo.Policies SET IsActive = 0 WHERE PolicyId = @PolicyId;
END
GO
