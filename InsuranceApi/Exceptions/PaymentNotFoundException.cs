namespace InsuranceApi.Exceptions;

public class PaymentNotFoundException: Exception
{
    public PaymentNotFoundException(int paymentId) : base($"Payment {paymentId} was not found.") {}
}