using InsuranceApi.DTOs;

namespace InsuranceApi.Services;

public interface IPaymentService
{
    Task<List<PaymentDto>?> GetScheduleAsync(int enrollmentId, int userId, bool isAdmin);   // null => 404
    Task<PaymentDto> PayAsync(int paymentId, int userId);
}