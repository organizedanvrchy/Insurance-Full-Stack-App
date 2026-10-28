using Insurance.BuildingBlocks.Exceptions;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.DTOs;
using Insurance.Enrollments.Enums;
using Insurance.Enrollments.Models;
using Insurance.Enrollments.Services;
using Xunit;

namespace Insurance.Enrollments.Tests;

public class EnrollmentServiceTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly FakeEnrollmentRepository _repo = new();
    private readonly EnrollmentService _sut;

    public EnrollmentServiceTests()
    {
        _sut = new EnrollmentService(_repo);
    }

    // Start date window

    [Theory]
    [InlineData(-1)]    // yesterday is allowed: one day of slack for time zones
    [InlineData(0)]
    [InlineData(90)]    // the furthest allowed
    public async Task Start_dates_inside_the_window_are_accepted(int daysFromToday)
    {
        var start = Today.AddDays(daysFromToday);

        var dto = await _sut.EnrollAsync(1, new EnrollmentRequest { QuoteId = 7, StartDate = start });

        Assert.Equal(start, dto.StartDate);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(91)]
    public async Task Start_dates_outside_the_window_are_rejected(int daysFromToday)
    {
        var request = new EnrollmentRequest { QuoteId = 7, StartDate = Today.AddDays(daysFromToday) };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EnrollAsync(1, request));
        Assert.Null(_repo.LastCreate);    // and nothing was sent to the database
    }

    [Fact]
    public async Task Enrolling_passes_the_callers_id_and_the_quote_to_the_repository()
    {
        await _sut.EnrollAsync(5, new EnrollmentRequest { QuoteId = 42, StartDate = Today });

        Assert.Equal(5, _repo.LastCreate!.Value.UserId);
        Assert.Equal(42, _repo.LastCreate!.Value.QuoteId);
    }

    [Fact]
    public async Task A_missing_start_date_defaults_to_today()
    {
        var before = Today;
        await _sut.EnrollAsync(1, new EnrollmentRequest { QuoteId = 7 });
        var after = Today;

        var used = _repo.LastCreate!.Value.StartDate;
        Assert.True(used == before || used == after);   // tolerates the clock ticking past midnight mid-test
    }

    // Payment alerts

    private static Enrollment Active(int overdue = 0, int? nextDueInDays = null) => new()
    {
        EnrollmentId = 1,
        UserId = 1,
        PolicyName = "Test Plan",
        Status = EnrollmentStatus.Active,
        OverdueCount = overdue,
        NextDueDate = nextDueInDays is { } days ? Today.AddDays(days) : null
    };

    private async Task<PaymentAlert> AlertFor(Enrollment enrollment)
    {
        _repo.Enrollments.Add(enrollment);
        var list = await _sut.GetMineAsync(1);
        return list.Single().PaymentAlert;
    }

    [Fact]
    public async Task Overdue_installments_raise_an_overdue_alert()
    {
        Assert.Equal(PaymentAlert.Overdue, await AlertFor(Active(overdue: 2, nextDueInDays: 30)));
    }

    [Theory]
    [InlineData(0, PaymentAlert.DueSoon)]     // due today
    [InlineData(3, PaymentAlert.DueSoon)]
    [InlineData(7, PaymentAlert.DueSoon)]     // last day of the window
    [InlineData(8, PaymentAlert.None)]        // just outside it
    [InlineData(30, PaymentAlert.None)]
    public async Task The_due_soon_window_is_seven_days(int nextDueInDays, PaymentAlert expected)
    {
        Assert.Equal(expected, await AlertFor(Active(nextDueInDays: nextDueInDays)));
    }

    [Fact]
    public async Task A_fully_paid_policy_has_no_alert()
    {
        Assert.Equal(PaymentAlert.None, await AlertFor(Active()));     // no next due date at all
    }

    [Fact]
    public async Task A_cancelled_policy_has_no_alert_even_with_overdue_history()
    {
        var cancelled = Active(overdue: 2, nextDueInDays: 1);
        cancelled.Status = EnrollmentStatus.Cancelled;

        Assert.Equal(PaymentAlert.None, await AlertFor(cancelled));
    }

    // Who can see an enrollment

    [Fact]
    public async Task The_owner_can_see_their_enrollment()
    {
        _repo.Enrollments.Add(Active());

        Assert.NotNull(await _sut.GetByIdAsync(1, userId: 1, isAdmin: false));
    }

    [Fact]
    public async Task Another_customer_gets_nothing_so_ids_cannot_be_probed()
    {
        _repo.Enrollments.Add(Active());

        Assert.Null(await _sut.GetByIdAsync(1, userId: 2, isAdmin: false));
    }

    [Fact]
    public async Task An_admin_can_see_any_enrollment()
    {
        _repo.Enrollments.Add(Active());

        Assert.NotNull(await _sut.GetByIdAsync(1, userId: 99, isAdmin: true));
    }

    // Cancel

    [Fact]
    public async Task The_owner_can_cancel_an_active_enrollment()
    {
        _repo.Enrollments.Add(Active());

        Assert.True(await _sut.CancelAsync(1, userId: 1));
        Assert.Equal(EnrollmentStatus.Cancelled, _repo.Enrollments.Single().Status);
    }

    [Fact]
    public async Task Someone_else_cannot_cancel_it()
    {
        _repo.Enrollments.Add(Active());

        Assert.False(await _sut.CancelAsync(1, userId: 2));
        Assert.Equal(EnrollmentStatus.Active, _repo.Enrollments.Single().Status);
    }
}