using Insurance.BuildingBlocks.Exceptions;

namespace Insurance.Enrollments.Exceptions;

public class DuplicateEnrollmentException : ConflictException
{
    public DuplicateEnrollmentException() : base("You already have an active enrollment policy.") {}
}