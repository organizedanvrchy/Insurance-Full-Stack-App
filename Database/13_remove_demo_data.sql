/* Removes the demo customers from every module's schema (dev tooling, like 12_seed_demo_data.sql). */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE c  FROM claims.Claims c
          JOIN accounts.Users u  ON u.UserId = c.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE pp FROM billing.PremiumPayments pp
          JOIN enrollments.Enrollments e ON e.EnrollmentId = pp.EnrollmentId
          JOIN accounts.Users u          ON u.UserId = e.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE q  FROM quotes.Quotes q
          JOIN accounts.Users u ON u.UserId = q.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE e  FROM enrollments.Enrollments e
          JOIN accounts.Users u ON u.UserId = e.UserId
          WHERE u.Email LIKE N'%@demo.example';

DELETE FROM accounts.Users WHERE Email LIKE N'%@demo.example';

COMMIT TRANSACTION;
PRINT 'Demo data removed.';
GO