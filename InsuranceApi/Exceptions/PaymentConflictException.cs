namespace InsuranceApi.Exceptions;

/// <summary>The payment exists but its current state doesn't allow this action (already paid, out of order, enrollment inactive).</summary>
public class PaymentConflictException: Exception
{
    public PaymentConflictException(string message) : base(message) {}
}