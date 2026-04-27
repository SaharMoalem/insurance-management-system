using Insurance.Api.Contracts.Policies;
using Insurance.Api.Data;
using Insurance.Api.Domain.Entities;
using Insurance.Api.Domain.Enums;
using Insurance.Api.Domain.Exceptions;
using Insurance.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Insurance.Api.Tests.Services;

public class PolicyServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var service = new PolicyService(dbContext);
        var request = BuildCreateRequest(9999, "POL-4040", PolicyType.Auto);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.CreateAsync(request));

        Assert.Equal("customer_not_found", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenStartDateAfterEndDate_ThrowsValidation()
    {
        await using var dbContext = CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext, "date-validation@example.com");
        var service = new PolicyService(dbContext);
        var request = BuildCreateRequest(customer.Id, "POL-DATE-001", PolicyType.Home);
        request.StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2));
        request.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Equal("invalid_policy_date_range", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateAsync_WhenPremiumIsZeroOrNegative_ThrowsValidation(decimal premiumAmount)
    {
        await using var dbContext = CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext, $"premium-{Guid.NewGuid():N}@example.com");
        var service = new PolicyService(dbContext);
        var request = BuildCreateRequest(customer.Id, $"POL-PREM-{Guid.NewGuid():N}", PolicyType.Life);
        request.PremiumAmount = premiumAmount;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.Equal("invalid_premium_amount", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenPolicyNumberExists_ThrowsConflictWithDuplicatePolicyNumberCode()
    {
        await using var dbContext = CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext, "duplicate-number@example.com");
        dbContext.Policies.Add(new Policy
        {
            PolicyNumber = "POL-1000",
            Type = PolicyType.Auto,
            Status = PolicyStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
            PremiumAmount = 100m,
            CustomerId = customer.Id
        });
        await dbContext.SaveChangesAsync();

        var service = new PolicyService(dbContext);
        var request = BuildCreateRequest(customer.Id, "POL-1000", PolicyType.Home);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));

        Assert.Equal("duplicate_policy_number", exception.Code);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateActivePolicyTypeExists_ThrowsConflictWithExpectedCode()
    {
        await using var dbContext = CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext, "duplicate-type@example.com");
        dbContext.Policies.Add(new Policy
        {
            PolicyNumber = "POL-2000",
            Type = PolicyType.Health,
            Status = PolicyStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
            PremiumAmount = 500m,
            CustomerId = customer.Id
        });
        await dbContext.SaveChangesAsync();

        var service = new PolicyService(dbContext);
        var request = BuildCreateRequest(customer.Id, "POL-2001", PolicyType.Health);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));

        Assert.Equal("duplicate_active_policy_type", exception.Code);
    }

    [Fact]
    public async Task CancelAsync_WhenPolicyIsActive_TransitionsToCancelled()
    {
        await using var dbContext = CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext, "cancel-policy@example.com");
        var policy = new Policy
        {
            PolicyNumber = "POL-3000",
            Type = PolicyType.Travel,
            Status = PolicyStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
            PremiumAmount = 250m,
            CustomerId = customer.Id
        };
        dbContext.Policies.Add(policy);
        await dbContext.SaveChangesAsync();

        var service = new PolicyService(dbContext);

        var response = await service.CancelAsync(policy.Id);

        Assert.Equal(PolicyStatus.Cancelled, response.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenPolicyDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var service = new PolicyService(dbContext);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.CancelAsync(9999));

        Assert.Equal("policy_not_found", exception.Code);
    }

    [Fact]
    public async Task CancelAsync_WhenPolicyAlreadyCancelled_ThrowsConflict()
    {
        await using var dbContext = CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext, "already-cancelled@example.com");
        var policy = new Policy
        {
            PolicyNumber = "POL-CAN-001",
            Type = PolicyType.Auto,
            Status = PolicyStatus.Cancelled,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
            PremiumAmount = 321m,
            CustomerId = customer.Id
        };
        dbContext.Policies.Add(policy);
        await dbContext.SaveChangesAsync();

        var service = new PolicyService(dbContext);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CancelAsync(policy.Id));

        Assert.Equal("invalid_policy_status_transition", exception.Code);
    }

    private static CreatePolicyRequest BuildCreateRequest(int customerId, string policyNumber, PolicyType type)
    {
        return new CreatePolicyRequest
        {
            CustomerId = customerId,
            PolicyNumber = policyNumber,
            Type = type,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
            PremiumAmount = 999m
        };
    }

    private static async Task<Customer> SeedCustomerAsync(InsuranceDbContext dbContext, string email)
    {
        var customer = new Customer
        {
            FullName = "Policy Test Customer",
            Email = email
        };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    private static InsuranceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InsuranceDbContext(options);
    }
}
