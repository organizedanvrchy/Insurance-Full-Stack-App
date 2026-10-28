using Insurance.Billing.Enums;
using Insurance.Billing.Exceptions;
using Insurance.Billing.Models;
using Insurance.Billing.Services;
using Insurance.Enrollments.Contracts;
using Xunit;

namespace Insurance.Billing.Tests;

// The rules that need another module's data (ownership, enrollment status) live in PaymentService.
// "Pay in order" and the double-click guard stay in usp_Payment_Pay, where the row is locked.

public class PaymentServiceTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly FakePaymentRepository _repo = new();
    private readonly FakeEnrollmentLookup _enrollments = new();
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _sut = new PaymentService(_repo, _enrollments);

        // Customer 1 holds enrollment 10, with payment 100 due today.
        SetEnrollment(EnrollmentStatus.Active);
        _repo.Payments.Add(new Payment { PaymentId = 100, EnrollmentId = 10, DueDate = Today, Amount = 50m, Status = PaymentStatus.Pending });
    }

    private void SetEnrollment(EnrollmentStatus status) =>
        _enrollments.Enrollments[10] = new EnrollmentInfo(10, UserId: 1, PolicyId: 1, Today.AddMonths(-1), Today.AddMonths(11), status);

    [Fact]
    public async Task The_owner_can_pay_an_installment()
    {
        var dto = await _sut.PayAsync(100, userId: 1);

        Assert.Equal(PaymentStatus.Paid, dto.Status);
        Assert.Equal(100, _repo.LastPaid);
    }

    [Theory]
    [InlineData(999, 1)]   // no such payment
    [InlineData(100, 2)]   // someone else's
    public async Task Missing_and_foreign_payments_look_the_same(int paymentId, int userId)
    {
        await Assert.ThrowsAsync<PaymentNotFoundException>(() => _sut.PayAsync(paymentId, userId));
        Assert.Null(_repo.LastPaid);
    }

    [Fact]
    public async Task A_paid_installment_cannot_be_paid_again()
    {
        _repo.Payments.Single().PaidDate = Today;

        var ex = await Assert.ThrowsAsync<PaymentConflictException>(() => _sut.PayAsync(100, userId: 1));
        Assert.Contains("already been paid", ex.Message);
    }

    [Fact]
    public async Task A_cancelled_policy_takes_no_payments()
    {
        SetEnrollment(EnrollmentStatus.Cancelled);

        await Assert.ThrowsAsync<PaymentConflictException>(() => _sut.PayAsync(100, userId: 1));
        Assert.Null(_repo.LastPaid);
    }

    [Fact]
    public async Task An_expired_policy_can_still_settle_its_last_installments()
    {
        SetEnrollment(EnrollmentStatus.Expired);

        await _sut.PayAsync(100, userId: 1);

        Assert.Equal(100, _repo.LastPaid);
    }

    [Theory]
    [InlineData(1, false, true)]    // owner
    [InlineData(2, false, false)]   // another customer
    [InlineData(2, true, true)]     // admin
    public async Task Who_can_see_the_schedule(int userId, bool isAdmin, bool visible)
    {
        var schedule = await _sut.GetScheduleAsync(10, userId, isAdmin);

        Assert.Equal(visible, schedule is not null);
    }
}
