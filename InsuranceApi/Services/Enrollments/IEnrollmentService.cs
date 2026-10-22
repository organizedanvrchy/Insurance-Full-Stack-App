using InsuranceApi.DTOs;

namespace InsuranceApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto> EnrollAsync(int userId, EnrollmentRequest request);
    Task<List<EnrollmentDto>> GetMineAsync(int userId);
    Task<EnrollmentDto?> GetByIdAsync(int enrollmentId, int userId, bool isAdmin);
    Task<bool> CancelAsync(int enrollmentId, int userId);
}