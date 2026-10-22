using System.ComponentModel.DataAnnotations;
using InsuranceApi.Enums;

namespace InsuranceApi.DTOs;

public class PolicyRequest
{
    [Required, StringLength(100)]
    public string PolicyName { get; set; } = string.Empty;

    [Required, RegularExpression("^(Health|Auto|Life|Home|Travel)$", ErrorMessage = "PolicyType must be Health, Auto, Life, Home or Travel.")]
    public PolicyType? PolicyType { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0.01, 100_000_000)]
    public decimal CoverageAmount { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal MonthlyPremium { get; set; }

    [Range(1, 600)]
    public int TermMonths { get; set; }
}
