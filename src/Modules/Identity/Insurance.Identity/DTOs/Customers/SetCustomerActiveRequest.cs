using System.ComponentModel.DataAnnotations;

namespace Insurance.Identity.DTOs;

public class SetCustomerActiveRequest
{
    [Required]      // nullable so a missing field is a 400
    public bool? IsActive { get; set; }
}