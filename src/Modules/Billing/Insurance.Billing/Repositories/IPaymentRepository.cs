using Insurance.Billing.Models;

namespace Insurance.Billing.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetByEnrollmentAsync(int enrollmentId);
    Task<Payment> PayAsync(int paymentId, int userId);   // throws PaymentNotFound / PaymentConflict
}