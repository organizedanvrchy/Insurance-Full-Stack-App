using Insurance.BuildingBlocks.Exceptions;

namespace Insurance.Billing.Exceptions;

public class PaymentNotFoundException : NotFoundException
{
    public PaymentNotFoundException(int paymentId) : base($"Payment {paymentId} was not found.") {}
}