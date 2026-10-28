using Insurance.Billing.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Repositories;
using MassTransit;

namespace Insurance.Enrollments.Messaging;

// The enrollment saga's last step, orchestrated here. The saga's state is the enrollment's Status column.
// Both consumers are safe to receive twice: completing an already-completed enrollment changes nothing,
// and the outcome event is simply published again (its consumers are idempotent too).

/// <summary>Billing created the schedule: Pending -> Active, and tell everyone.</summary>
public class PremiumScheduleCreatedConsumer : IConsumer<PremiumScheduleCreated>
{
    private readonly IEnrollmentRepository _enrollments;
    public PremiumScheduleCreatedConsumer(IEnrollmentRepository enrollments) => _enrollments = enrollments;

    public async Task Consume(ConsumeContext<PremiumScheduleCreated> context)
    {
        var e = await _enrollments.CompleteAsync(context.Message.EnrollmentId, succeeded: true);
        if (e?.Status is EnrollmentStatus.Active or EnrollmentStatus.Expired)
            await context.Publish(new EnrollmentActivated(e.EnrollmentId, e.UserId, e.PolicyId, e.QuoteId));
    }
}

/// <summary>Billing gave up on the schedule (MassTransit's Fault after the last retry): Pending -> Failed, and undo.</summary>
public class EnrollmentRequestedFaultConsumer : IConsumer<Fault<EnrollmentRequested>>
{
    private readonly IEnrollmentRepository _enrollments;
    public EnrollmentRequestedFaultConsumer(IEnrollmentRepository enrollments) => _enrollments = enrollments;

    public async Task Consume(ConsumeContext<Fault<EnrollmentRequested>> context)
    {
        var e = await _enrollments.CompleteAsync(context.Message.Message.EnrollmentId, succeeded: false);
        if (e?.Status == EnrollmentStatus.Failed)
            await context.Publish(new EnrollmentFailed(e.EnrollmentId, e.QuoteId));
    }
}
