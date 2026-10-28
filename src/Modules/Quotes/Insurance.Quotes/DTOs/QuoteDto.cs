using Insurance.Catalog.Contracts;
using Insurance.Quotes.Enums;
using Insurance.Quotes.Rating;

namespace Insurance.Quotes.DTOs;

public class QuoteDto
{
    public int QuoteId { get; set; }
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public PolicyType PolicyType { get; set; }
    public decimal CoverageAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal BasePremium { get; set; }
    public decimal MonthlyPremium { get; set; }
    public decimal TotalOverTerm { get; set; }
    public IReadOnlyList<RatingFactor> Factors { get; set; } = new List<RatingFactor>();
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsUsed { get; set; }
}