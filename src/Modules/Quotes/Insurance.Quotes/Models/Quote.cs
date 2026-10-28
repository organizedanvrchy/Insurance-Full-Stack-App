
namespace Insurance.Quotes.Models;

public class Quote
{
    public int QuoteId { get; set; }
    public int UserId { get; set; }
    public int PolicyId { get; set; }
    public decimal BasePremium { get; set; }
    public decimal MonthlyPremium { get; set; }
    public string Breakdown { get; set; } = "[]";      // JSON list of rating factors
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int? EnrollmentId { get; set; }             // set once the quote has been used
}