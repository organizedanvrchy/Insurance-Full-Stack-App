namespace InsuranceApi.Exceptions;

public class PolicyNotAvailableException: Exception
{
    public PolicyNotAvailableException (int policyId) : base($"Policy {policyId} does not exist or is no longer available.") {}
}