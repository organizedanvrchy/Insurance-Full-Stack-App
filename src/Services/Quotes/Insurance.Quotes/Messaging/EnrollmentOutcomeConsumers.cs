using Insurance.Enrollments.Contracts;
using Insurance.Quotes.Services;
using MassTransit;

namespace Insurance.Quotes.Messaging;

// The end of the enrollment saga, as seen by the Quotes service. Both are safe to receive twice.

/// <summary>The enrollment is active: the reserved quote is now used for good.</summary>
public class EnrollmentActivatedConsumer : IConsumer<EnrollmentActivated>
{
    private readonly QuoteReservations _quotes;
    public EnrollmentActivatedConsumer(QuoteReservations quotes) => _quotes = quotes;

    public Task Consume(ConsumeContext<EnrollmentActivated> context) =>
        _quotes.MarkUsedAsync(context.Message.QuoteId, context.Message.EnrollmentId);
}

/// <summary>The enrollment failed: the customer can use the quote again.</summary>
public class EnrollmentFailedConsumer : IConsumer<EnrollmentFailed>
{
    private readonly QuoteReservations _quotes;
    public EnrollmentFailedConsumer(QuoteReservations quotes) => _quotes = quotes;

    public Task Consume(ConsumeContext<EnrollmentFailed> context) =>
        _quotes.ReleaseAsync(context.Message.QuoteId);
}
