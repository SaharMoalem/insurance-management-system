using Insurance.Api.Domain.Enums;

namespace Insurance.Api.Domain.Entities;

public class Policy
{
    public int Id { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public PolicyType Type { get; set; }

    public PolicyStatus Status { get; set; } = PolicyStatus.Active;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal PremiumAmount { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }
}
