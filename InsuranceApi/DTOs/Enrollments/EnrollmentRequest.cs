using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.DTOs;

public class EnrollmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "QuoteId is required.")]
    public int QuoteId { get; set; }

    // Optional. Defaults to today. 
    // Service checks allowed range.
    public DateOnly? StartDate { get; set; }
}