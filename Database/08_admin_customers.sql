/* One row per customer, with the counts an admin cares about. Admin accounts never appear here. */
CREATE OR ALTER VIEW dbo.vw_CustomerSummary
AS
SELECT  u.UserId, u.FullName, u.Email, u.Phone, u.IsActive, u.CreatedAt,
        (SELECT COUNT(*) FROM dbo.Enrollments e
         WHERE e.UserId = u.UserId AND e.Status = 'Active'
           AND e.EndDate >= CAST(SYSUTCDATETIME() AS DATE))                         AS ActivePolicies,
        (SELECT COUNT(*) FROM dbo.Claims c
         JOIN dbo.Enrollments e ON e.EnrollmentId = c.EnrollmentId
         WHERE e.UserId = u.UserId)                                                  AS TotalClaims,
        (SELECT COUNT(*) FROM dbo.Claims c
         JOIN dbo.Enrollments e ON e.EnrollmentId = c.EnrollmentId
         WHERE e.UserId = u.UserId AND c.Status IN ('Submitted', 'UnderReview'))     AS OpenClaims
FROM dbo.Users u
WHERE u.Role = 'Customer';
GO

/* Optional filters: pass NULL to skip one. */
CREATE OR ALTER PROCEDURE dbo.usp_Customer_GetAll
    @Search   NVARCHAR(100) = NULL,
    @IsActive BIT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_CustomerSummary
    WHERE (@IsActive IS NULL OR IsActive = @IsActive)
      AND (@Search IS NULL OR FullName LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')
    ORDER BY FullName;
END
GO

/* The Role = 'Customer' filter in the UPDATE is the safety net: admins can't be deactivated here.
   Returns the updated row, or no row at all if the id isn't a customer (the API turns that into a 404). */
CREATE OR ALTER PROCEDURE dbo.usp_Customer_SetActive
    @UserId   INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET IsActive = @IsActive
    WHERE UserId = @UserId AND Role = 'Customer';

    SELECT * FROM dbo.vw_CustomerSummary WHERE UserId = @UserId;
END
GO