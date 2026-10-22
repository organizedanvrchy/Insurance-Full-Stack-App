namespace InsuranceApi.Exceptions;

public class DuplicateEnrollmentException: Exception
{
    public DuplicateEnrollmentException() : base("You already have an active enrollment policy.") {}
}