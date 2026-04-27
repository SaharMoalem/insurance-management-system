using Insurance.Api.Domain.Enums;

namespace Insurance.Api.Contracts.Policies;

public class PolicyResponse
{
    public int Id { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public PolicyType Type { get; set; }

    public PolicyStatus Status { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal PremiumAmount { get; set; }

    public int CustomerId { get; set; }
}
