using Insurance.BuildingBlocks.Exceptions;
using Insurance.Claims.DTOs;
using Insurance.Claims.Enums;
using Insurance.Claims.Exceptions;
using Insurance.Claims.Models;
using Insurance.Claims.Services;
using Xunit;

namespace Insurance.Claims.Tests;

public class ClaimServiceTests
{
    private readonly FakeClaimRepository _repo = new();
    private readonly ClaimService _sut;

    public ClaimServiceTests()
    {
        _sut = new ClaimService(_repo);
    }

    private InsuranceClaim AddClaim(ClaimStatus status, int ownerId = 1)
    {
        var claim = new InsuranceClaim
        {
            ClaimId = _repo.Claims.Count + 1,
            ClaimNumber = "CLM-TEST-000001",
            UserId = ownerId,
            EnrollmentId = 1,
            Status = status,
            ClaimAmount = 500m,
            Description = "Test claim"
        };
        _repo.Claims.Add(claim);
        return claim;
    }

    private static ClaimReviewRequest Review(ClaimStatus to, string? notes = null) =>
        new() { Status = to, AdminNotes = notes };

    // Submit

    [Fact]
    public async Task Submitting_trims_the_description_and_shows_no_admin_actions_to_the_customer()
    {
        var request = new ClaimSubmitRequest
        {
            EnrollmentId = 5,
            IncidentDate = new DateOnly(2026, 10, 1),
            Description = "  Water damage in the kitchen  ",
            ClaimAmount = 500m
        };

        var dto = await _sut.SubmitAsync(userId: 3, request);

        Assert.Equal("Water damage in the kitchen", _repo.Claims.Single().Description);
        Assert.Equal(ClaimStatus.Submitted, dto.Status);
        Assert.Empty(dto.AllowedNextStatuses);
    }

    // Review

    [Fact]
    public async Task Reviewing_a_claim_that_does_not_exist_is_a_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.ReviewAsync(999, adminId: 1, Review(ClaimStatus.UnderReview)));
    }

    [Fact]
    public async Task Skipping_a_step_in_the_workflow_is_a_conflict()
    {
        AddClaim(ClaimStatus.Submitted);

        await Assert.ThrowsAsync<ClaimConflictException>(
            () => _sut.ReviewAsync(1, adminId: 1, Review(ClaimStatus.Approved)));

        Assert.Null(_repo.LastReview);    // the database was never called
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejecting_requires_a_reason(string? notes)
    {
        AddClaim(ClaimStatus.UnderReview);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.ReviewAsync(1, adminId: 1, Review(ClaimStatus.Rejected, notes)));
    }

    [Fact]
    public async Task Rejecting_with_a_reason_works_and_leaves_no_further_actions()
    {
        AddClaim(ClaimStatus.UnderReview);

        var dto = await _sut.ReviewAsync(1, adminId: 1, Review(ClaimStatus.Rejected, "Not covered"));

        Assert.Equal(ClaimStatus.Rejected, dto.Status);
        Assert.Empty(dto.AllowedNextStatuses);
    }

    [Fact]
    public async Task The_service_passes_the_status_it_just_read_as_the_from_status()
    {
        // This is the optimistic-concurrency guard: 
        // the stored procedure only updates the claim
        // if it is STILL in this status, so two admins 
        // can't overwrite each other.
        AddClaim(ClaimStatus.UnderReview);

        var dto = await _sut.ReviewAsync(1, adminId: 7, Review(ClaimStatus.Approved, "Looks good"));

        var call = _repo.LastReview!.Value;
        Assert.Equal(ClaimStatus.UnderReview, call.From);
        Assert.Equal(ClaimStatus.Approved, call.To);
        Assert.Equal(7, call.ReviewerId);
        Assert.Equal("Looks good", call.Notes);
        Assert.Equal(new[] { ClaimStatus.Paid }, dto.AllowedNextStatuses);   // the next legal step
    }

    // Who can see a claim

    [Fact]
    public async Task The_owner_sees_their_claim_without_admin_actions()
    {
        AddClaim(ClaimStatus.Submitted, ownerId: 1);

        var dto = await _sut.GetByIdAsync(1, userId: 1, isAdmin: false);

        Assert.NotNull(dto);
        Assert.Empty(dto!.AllowedNextStatuses);
    }

    [Fact]
    public async Task Another_customer_cannot_see_the_claim()
    {
        AddClaim(ClaimStatus.Submitted, ownerId: 1);

        Assert.Null(await _sut.GetByIdAsync(1, userId: 2, isAdmin: false));
    }

    [Fact]
    public async Task An_admin_sees_the_claim_with_the_available_next_steps()
    {
        AddClaim(ClaimStatus.Submitted, ownerId: 1);

        var dto = await _sut.GetByIdAsync(1, userId: 99, isAdmin: true);

        Assert.NotNull(dto);
        Assert.Equal(new[] { ClaimStatus.UnderReview }, dto!.AllowedNextStatuses);
    }

    [Fact]
    public async Task The_admin_list_can_be_filtered_by_status()
    {
        AddClaim(ClaimStatus.Submitted);
        AddClaim(ClaimStatus.Paid);

        var submitted = await _sut.GetAllAsync(ClaimStatus.Submitted);

        Assert.Single(submitted);
    }
}