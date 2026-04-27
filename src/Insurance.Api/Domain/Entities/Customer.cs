namespace Insurance.Api.Domain.Entities;

public class Customer
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
