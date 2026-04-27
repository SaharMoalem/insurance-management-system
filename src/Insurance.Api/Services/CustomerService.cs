using Insurance.Api.Contracts.Customers;
using Insurance.Api.Data;
using Insurance.Api.Domain.Entities;
using Insurance.Api.Domain.Exceptions;
using Insurance.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Insurance.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly InsuranceDbContext _dbContext;

    public CustomerService(InsuranceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        await EnsureEmailUniqueAsync(normalizedEmail, null, cancellationToken);

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim()
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("customer_not_found", $"Customer with id '{id}' was not found.");
        }

        return Map(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("customer_not_found", $"Customer with id '{id}' was not found.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        await EnsureEmailUniqueAsync(normalizedEmail, id, cancellationToken);

        customer.FullName = request.FullName.Trim();
        customer.Email = normalizedEmail;
        customer.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(customer);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("customer_not_found", $"Customer with id '{id}' was not found.");
        }

        var hasRelatedPolicies = await _dbContext.Policies
            .AnyAsync(x => x.CustomerId == id, cancellationToken);
        if (hasRelatedPolicies)
        {
            throw new ConflictException(
                "customer_has_active_policies",
                $"Customer with id '{id}' cannot be deleted because related policies exist.");
        }

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEmailUniqueAsync(string email, int? existingCustomerId, CancellationToken cancellationToken)
    {
        var query = _dbContext.Customers.AsQueryable();
        if (existingCustomerId.HasValue)
        {
            query = query.Where(x => x.Id != existingCustomerId.Value);
        }

        var exists = await query.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            throw new ConflictException("duplicate_email", $"A customer with email '{email}' already exists.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static CustomerResponse Map(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber
        };
    }
}
