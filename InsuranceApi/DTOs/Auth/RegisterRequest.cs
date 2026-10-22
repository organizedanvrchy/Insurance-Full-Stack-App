using System.ComponentModel.DataAnnotations;

namespace InsuranceApi.DTOs;

public class RegisterRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(72, MinimumLength = 8)]   // BCrypt only uses the first 72 bytes
    public string Password { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }
}