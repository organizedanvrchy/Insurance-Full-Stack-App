using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog.Contracts;
using Insurance.Claims.DTOs;
using Insurance.Claims.Enums;
using Insurance.Claims.Exceptions;
using Insurance.Claims.Models;
using Insurance.Claims.Repositories;

namespace Insurance.Claims.Services;

public class ClaimService : IClaimService
{
    private readonly IClaimRepository _claims;
    public ClaimService(IClaimRepository claims) => _claims = claims;

    public async Task<ClaimDto> SubmitAsync(int userId, ClaimSubmitRequest request)
    {
        // Ownership, dates, overdue premiums and coverage limits are all enforced inside usp_Claim_Submit.
        var id = await _claims.SubmitAsync(
            userId, request.EnrollmentId, request.IncidentDate!.Value, request.Description.Trim(), request.ClaimAmount);

        var created = await _claims.GetByIdAsync(id);
        return MapToDto(created!, isAdmin: false);
    }

    public async Task<List<ClaimDto>> GetMineAsync(int userId)
    {
        var list = await _claims.GetByUserAsync(userId);
        return list.Select(c => MapToDto(c, isAdmin: false)).ToList();
    }

    public async Task<List<ClaimDto>> GetAllAsync(ClaimStatus? status)
    {
        var list = await _claims.GetAllAsync(status);
        return list.Select(c => MapToDto(c, isAdmin: true)).ToList();
    }

    public async Task<ClaimDto?> GetByIdAsync(int claimId, int userId, bool isAdmin)
    {
        var claim = await _claims.GetByIdAsync(claimId);

        // Same null for "doesn't exist" and "belongs to someone else".
        if (claim is null || (claim.UserId != userId && !isAdmin))
            return null;

        return MapToDto(claim, isAdmin);
    }

    public async Task<ClaimDto> ReviewAsync(int claimId, int adminId, ClaimReviewRequest request)
    {
        var claim = await _claims.GetByIdAsync(claimId)
            ?? throw new NotFoundException("Claim not found.");

        var target = request.Status!.Value;
        var notes = request.AdminNotes?.Trim();

        if (!ClaimStatusTransitions.CanMove(claim.Status, target))
            throw new ClaimConflictException($"A claim that is {claim.Status} cannot be moved to {target}.");

        if (target == ClaimStatus.Rejected && string.IsNullOrWhiteSpace(notes))
            throw new BusinessRuleException("Please give a reason when rejecting a claim.");

        // We pass the status we just read as the "from" status. If someone else changes the claim
        // between our read and this call, the proc finds no matching row and we get a 409.
        var updated = await _claims.ReviewAsync(claimId, claim.Status, target, notes, adminId);
        return MapToDto(updated, isAdmin: true);
    }

    private static ClaimDto MapToDto(InsuranceClaim c, bool isAdmin) => new()
    {
        ClaimId = c.ClaimId,
        ClaimNumber = c.ClaimNumber,
        EnrollmentId = c.EnrollmentId,
        PolicyName = c.PolicyName,
        PolicyType = c.PolicyType,
        CustomerName = c.CustomerName,
        CustomerEmail = c.CustomerEmail,
        IncidentDate = c.IncidentDate,
        Description = c.Description,
        ClaimAmount = c.ClaimAmount,
        Status = c.Status,
        AdminNotes = c.AdminNotes,
        SubmittedAt = c.SubmittedAt,
        ReviewedAt = c.ReviewedAt,
        ReviewedBy = c.ReviewedByName,
        AllowedNextStatuses = isAdmin ? ClaimStatusTransitions.NextFrom(c.Status).ToList() : new List<ClaimStatus>()
    };
}