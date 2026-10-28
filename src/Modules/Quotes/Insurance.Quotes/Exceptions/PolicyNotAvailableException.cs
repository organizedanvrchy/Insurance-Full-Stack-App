using Insurance.BuildingBlocks.Exceptions;

namespace Insurance.Quotes.Exceptions;

public class PolicyNotAvailableException : NotFoundException
{
    public PolicyNotAvailableException (int policyId) : base($"Policy {policyId} does not exist or is no longer available.") {}
}