using InsuranceApi.Models;

namespace InsuranceApi.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetByEnrollmentAsync(int enrollmentId);
    Task<Payment> PayAsync(int paymentId, int userId);   // throws PaymentNotFound / PaymentConflict
}