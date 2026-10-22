namespace InsuranceApi.Exceptions;

/// <summary>Something the caller asked for doesn't exist (or isn't theirs). Becomes a 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}