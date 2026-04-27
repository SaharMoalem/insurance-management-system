using System.ComponentModel.DataAnnotations;
using Insurance.Api.Domain.Enums;

namespace Insurance.Api.Contracts.Policies;

public class CreatePolicyRequest
{
    [Required]
    [StringLength(100)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(PolicyType))]
    public PolicyType Type { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal PremiumAmount { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }
}
