using System.ComponentModel.DataAnnotations;

namespace Insurance.Api.Contracts.Customers;

public class CreateCustomerRequest
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }
}
