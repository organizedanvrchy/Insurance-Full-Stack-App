/* Identity service database (InsuranceIdentity). Run automatically by the service at startup
   (DatabaseMigrator): each script runs once, in name order, and is recorded in dbo.SchemaVersions.
   Only the Identity service reads or writes this database. */

CREATE SCHEMA accounts;
GO

CREATE TABLE accounts.Users (
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
GO

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

/* For other services (claim screens show customer and reviewer names). @UserIds is a JSON array: [1,2,3]. */
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

/* Customers only (admins never appear). The policy and claim counts are added by the gateway (BFF)
   from the Enrollments and Claims services. Optional filters: pass NULL to skip one. */
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
   Returns the updated row, or no row at all if the id isn't a customer (the service turns that into a 404). */
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
