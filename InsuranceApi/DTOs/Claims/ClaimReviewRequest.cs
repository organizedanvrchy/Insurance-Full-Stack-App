using System.ComponentModel.DataAnnotations;
using InsuranceApi.Enums;

namespace InsuranceApi.DTOs;

public class ClaimReviewRequest
{
    [Required]
    public ClaimStatus? Status { get; set; }        // the status to move TO

    [StringLength(1000)]
    public string? AdminNotes { get; set; }
}