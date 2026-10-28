namespace Insurance.BuildingBlocks.Exceptions;

/// <summary>
/// The request conflicts with the current state of something. 
/// Becomes a 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}