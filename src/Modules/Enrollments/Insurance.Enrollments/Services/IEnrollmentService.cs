using Insurance.Enrollments.DTOs;

namespace Insurance.Enrollments.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto> EnrollAsync(int userId, EnrollmentRequest request);
    Task<List<EnrollmentDto>> GetMineAsync(int userId);
    Task<EnrollmentDto?> GetByIdAsync(int enrollmentId, int userId, bool isAdmin);
    Task<bool> CancelAsync(int enrollmentId, int userId);
}