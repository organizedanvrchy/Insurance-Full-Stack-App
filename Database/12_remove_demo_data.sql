SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE c  FROM dbo.Claims c
          JOIN dbo.Enrollments e ON e.EnrollmentId = c.EnrollmentId
          JOIN dbo.Users u       ON u.UserId = e.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE pp FROM dbo.PremiumPayments pp
          JOIN dbo.Enrollments e ON e.EnrollmentId = pp.EnrollmentId
          JOIN dbo.Users u       ON u.UserId = e.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE q  FROM dbo.Quotes q
          JOIN dbo.Users u ON u.UserId = q.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE e  FROM dbo.Enrollments e
          JOIN dbo.Users u ON u.UserId = e.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE FROM dbo.Users WHERE Email LIKE N'%@demo.example';

COMMIT TRANSACTION;
PRINT 'Demo data removed.';
GO