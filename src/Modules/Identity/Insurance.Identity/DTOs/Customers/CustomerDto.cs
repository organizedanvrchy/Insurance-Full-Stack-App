namespace Insurance.Identity.DTOs;

public class CustomerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActivePolicies { get; set; }
    public int TotalClaims { get; set; }
    public int OpenClaims { get; set; }
}