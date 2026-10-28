using Insurance.Billing.Contracts;
using Insurance.Billing.Repositories;
using Insurance.Enrollments.Contracts;
using MassTransit;

namespace Insurance.Billing.Messaging;

// Billing's part in the enrollment lifecycle. Every consumer is safe to receive the same message twice.

/// <summary>Saga step: create the premium schedule, then report back. A second delivery finds the schedule and just reports again.</summary>
public class EnrollmentRequestedConsumer : IConsumer<EnrollmentRequested>
{
    private readonly IPaymentRepository _payments;
    public EnrollmentRequestedConsumer(IPaymentRepository payments) => _payments = payments;

    public async Task Consume(ConsumeContext<EnrollmentRequested> context)
    {
        var m = context.Message;
        await _payments.CreateScheduleAsync(m.EnrollmentId, m.StartDate, m.TermMonths, m.MonthlyPremium);
        await context.Publish(new PremiumScheduleCreated(m.EnrollmentId));
    }
}

/// <summary>The customer cancelled: unpaid installments go, paid ones stay as history.</summary>
public class EnrollmentCancelledConsumer : IConsumer<EnrollmentCancelled>
{
    private readonly IPaymentRepository _payments;
    public EnrollmentCancelledConsumer(IPaymentRepository payments) => _payments = payments;

    public Task Consume(ConsumeContext<EnrollmentCancelled> context) =>
        _payments.RemoveUnpaidAsync(context.Message.EnrollmentId);
}

/// <summary>Compensation: the enrollment saga failed, so any schedule created for it goes (nothing was paid yet).</summary>
public class EnrollmentFailedConsumer : IConsumer<EnrollmentFailed>
{
    private readonly IPaymentRepository _payments;
    public EnrollmentFailedConsumer(IPaymentRepository payments) => _payments = payments;

    public Task Consume(ConsumeContext<EnrollmentFailed> context) =>
        _payments.RemoveUnpaidAsync(context.Message.EnrollmentId);
}
