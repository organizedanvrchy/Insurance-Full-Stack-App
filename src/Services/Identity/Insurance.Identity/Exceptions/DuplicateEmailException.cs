using Insurance.BuildingBlocks.Exceptions;

namespace Insurance.Identity.Exceptions;

public class DuplicateEmailException : ConflictException
{
    public string Email { get; }

    public DuplicateEmailException(string email) : base("That email is already registered.") => Email = email;
}
