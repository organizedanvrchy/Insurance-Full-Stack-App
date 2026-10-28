using Insurance.Catalog.Contracts;


namespace Insurance.Catalog.Models;

public class Policy
{
    public int PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public PolicyType PolicyType { get; set; }
    public string? Description { get; set; }
    public decimal CoverageAmount { get; set; }
    public decimal MonthlyPremium { get; set; }
    public int TermMonths { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
