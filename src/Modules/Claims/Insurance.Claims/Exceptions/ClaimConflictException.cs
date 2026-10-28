using Insurance.BuildingBlocks.Exceptions;

namespace Insurance.Claims.Exceptions;

/// <summary>The claim or enrollment is in a state that doesn't allow this action. Becomes a 409.</summary>
public class ClaimConflictException : ConflictException
{
    public ClaimConflictException(string message) : base(message) { }
}