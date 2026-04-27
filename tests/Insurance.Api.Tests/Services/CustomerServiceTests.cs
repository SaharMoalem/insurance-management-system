using Insurance.Api.Contracts.Customers;
using Insurance.Api.Data;
using Insurance.Api.Domain.Entities;
using Insurance.Api.Domain.Enums;
using Insurance.Api.Domain.Exceptions;
using Insurance.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Insurance.Api.Tests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesCustomer()
    {
        await using var dbContext = CreateDbContext();
        var service = new CustomerService(dbContext);
        var request = new CreateCustomerRequest
        {
            FullName = "Valid Customer",
            Email = "valid@example.com",
            PhoneNumber = "123456789"
        };

        var response = await service.CreateAsync(request);

        Assert.Equal("Valid Customer", response.FullName);
        Assert.Equal("valid@example.com", response.Email);

        var exists = await dbContext.Customers.AnyAsync(x => x.Id == response.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_ThrowsConflictWithDuplicateEmailCode()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Customers.Add(new Customer
        {
            FullName = "Existing User",
            Email = "existing@example.com"
        });
        await dbContext.SaveChangesAsync();

        var service = new CustomerService(dbContext);
        var request = new CreateCustomerRequest
        {
            FullName = "New User",
            Email = "existing@example.com"
        };

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));

        Assert.Equal("duplicate_email", exception.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var service = new CustomerService(dbContext);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(9999));

        Assert.Equal("customer_not_found", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailBelongsToAnotherCustomer_ThrowsConflict()
    {
        await using var dbContext = CreateDbContext();
        var customerA = new Customer
        {
            FullName = "Customer A",
            Email = "a@example.com"
        };
        var customerB = new Customer
        {
            FullName = "Customer B",
            Email = "b@example.com"
        };
        dbContext.Customers.AddRange(customerA, customerB);
        await dbContext.SaveChangesAsync();

        var service = new CustomerService(dbContext);
        var request = new UpdateCustomerRequest
        {
            FullName = "Customer A Updated",
            Email = "b@example.com",
            PhoneNumber = "999"
        };

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(customerA.Id, request));

        Assert.Equal("duplicate_email", exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerHasActivePolicy_ThrowsConflictWithExpectedCode()
    {
        await using var dbContext = CreateDbContext();
        var customer = new Customer
        {
            FullName = "Delete Guard Customer",
            Email = "delete-guard@example.com"
        };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        dbContext.Policies.Add(new Policy
        {
            PolicyNumber = "POL-DEL-001",
            Type = PolicyType.Auto,
            Status = PolicyStatus.Active,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
            PremiumAmount = 1000m,
            CustomerId = customer.Id
        });
        await dbContext.SaveChangesAsync();

        var service = new CustomerService(dbContext);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(customer.Id));

        Assert.Equal("customer_has_active_policies", exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoActivePolicies_DeletesCustomer()
    {
        await using var dbContext = CreateDbContext();
        var customer = new Customer
        {
            FullName = "Delete Success Customer",
            Email = "delete-success@example.com"
        };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        var service = new CustomerService(dbContext);

        await service.DeleteAsync(customer.Id);

        var exists = await dbContext.Customers.AnyAsync(x => x.Id == customer.Id);
        Assert.False(exists);
    }

    private static InsuranceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InsuranceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InsuranceDbContext(options);
    }
}
