/* Overdue is derived on read, so no job ever has to update it.
   We join Enrollments so the view carries UserId, which makes ownership checks easy. */
CREATE OR ALTER VIEW dbo.vw_PaymentDetails
AS
SELECT  pp.PaymentId, pp.EnrollmentId, e.UserId,
        pp.DueDate, pp.Amount, pp.PaidDate,
        CASE WHEN pp.Status = 'Pending' AND pp.DueDate < CAST(SYSUTCDATETIME() AS DATE)
             THEN 'Overdue' ELSE pp.Status END AS Status
FROM dbo.PremiumPayments pp
JOIN dbo.Enrollments e ON e.EnrollmentId = pp.EnrollmentId;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Payment_GetByEnrollment
    @EnrollmentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_PaymentDetails
    WHERE EnrollmentId = @EnrollmentId
    ORDER BY DueDate;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Payment_GetById
    @PaymentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.vw_PaymentDetails
    WHERE PaymentId = @PaymentId;
END
GO

/* Pay one installment. All checks and the update happen in one transaction, with the row locked,
   so two simultaneous "Pay" clicks can't both succeed.

   Error numbers used by this proc:
     50004  payment not found OR not yours (same message on purpose, so ids can't be probed)
     50005  already paid
     50006  enrollment is not active
     50007  an earlier installment is still unpaid */
CREATE OR ALTER PROCEDURE dbo.usp_Payment_Pay
    @PaymentId INT,
    @UserId    INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EnrollmentId INT, @OwnerId INT, @DueDate DATE, @PaidDate DATE, @EnrollmentStatus NVARCHAR(20);

    BEGIN TRANSACTION;

        SELECT @EnrollmentId = pp.EnrollmentId, @DueDate = pp.DueDate, @PaidDate = pp.PaidDate,
               @OwnerId = e.UserId, @EnrollmentStatus = e.Status
        FROM dbo.PremiumPayments pp WITH (UPDLOCK)
        JOIN dbo.Enrollments e ON e.EnrollmentId = pp.EnrollmentId
        WHERE pp.PaymentId = @PaymentId;

        IF @EnrollmentId IS NULL OR @OwnerId <> @UserId
            THROW 50004, 'Payment not found.', 1;

        IF @PaidDate IS NOT NULL
            THROW 50005, 'This installment has already been paid.', 1;

        IF @EnrollmentStatus <> 'Active'
            THROW 50006, 'Payments can only be made on an active enrollment.', 1;

        IF EXISTS (SELECT 1 FROM dbo.PremiumPayments
                   WHERE EnrollmentId = @EnrollmentId AND PaidDate IS NULL AND DueDate < @DueDate)
            THROW 50007, 'Please pay your earlier installments first.', 1;

        UPDATE dbo.PremiumPayments
        SET Status = 'Paid', PaidDate = CAST(SYSUTCDATETIME() AS DATE)
        WHERE PaymentId = @PaymentId;

    COMMIT TRANSACTION;

    SELECT * FROM dbo.vw_PaymentDetails WHERE PaymentId = @PaymentId;   -- return the updated row
END
GO