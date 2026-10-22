namespace InsuranceApi.Exceptions;

/// <summary>The claim or enrollment is in a state that doesn't allow this action. Becomes a 409.</summary>
public class ClaimConflictException : Exception
{
    public ClaimConflictException(string message) : base(message) { }
}