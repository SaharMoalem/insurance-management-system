using Insurance.Api.Contracts.Policies;
using Insurance.Api.Data;
using Insurance.Api.Domain.Entities;
using Insurance.Api.Domain.Exceptions;
using Insurance.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Insurance.Api.Services;

public class PolicyService : IPolicyService
{
    private readonly InsuranceDbContext _dbContext;

    public PolicyService(InsuranceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PolicyResponse> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureCustomerExistsAsync(request.CustomerId, cancellationToken);
        await EnsurePolicyNumberUniqueAsync(request.PolicyNumber, null, cancellationToken);
        ValidatePolicyDatesAndPremium(request.StartDate, request.EndDate, request.PremiumAmount);

        var policy = new Policy
        {
            PolicyNumber = request.PolicyNumber.Trim(),
            Type = request.Type,
            Status = Domain.Enums.PolicyStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            CustomerId = request.CustomerId
        };

        _dbContext.Policies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Policies
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.Policies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException($"Policy with id '{id}' was not found.");
        }

        return Map(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(int id, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.Policies
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException($"Policy with id '{id}' was not found.");
        }

        await EnsureCustomerExistsAsync(request.CustomerId, cancellationToken);
        await EnsurePolicyNumberUniqueAsync(request.PolicyNumber, id, cancellationToken);
        ValidatePolicyDatesAndPremium(request.StartDate, request.EndDate, request.PremiumAmount);

        policy.PolicyNumber = request.PolicyNumber.Trim();
        policy.Type = request.Type;
        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.PremiumAmount = request.PremiumAmount;
        policy.CustomerId = request.CustomerId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    private async Task EnsureCustomerExistsAsync(int customerId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Customers.AnyAsync(x => x.Id == customerId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Customer with id '{customerId}' was not found.");
        }
    }

    private async Task EnsurePolicyNumberUniqueAsync(string policyNumber, int? existingPolicyId, CancellationToken cancellationToken)
    {
        var normalizedPolicyNumber = policyNumber.Trim();

        var query = _dbContext.Policies.AsQueryable();
        if (existingPolicyId.HasValue)
        {
            query = query.Where(x => x.Id != existingPolicyId.Value);
        }

        var exists = await query.AnyAsync(x => x.PolicyNumber == normalizedPolicyNumber, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"A policy with number '{normalizedPolicyNumber}' already exists.");
        }
    }

    private static void ValidatePolicyDatesAndPremium(DateOnly startDate, DateOnly endDate, decimal premiumAmount)
    {
        if (startDate >= endDate)
        {
            throw new ValidationException("Policy start date must be earlier than end date.");
        }

        if (premiumAmount <= 0)
        {
            throw new ValidationException("Policy premium amount must be greater than zero.");
        }
    }

    private static PolicyResponse Map(Policy policy)
    {
        return new PolicyResponse
        {
            Id = policy.Id,
            PolicyNumber = policy.PolicyNumber,
            Type = policy.Type,
            Status = policy.Status,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            PremiumAmount = policy.PremiumAmount,
            CustomerId = policy.CustomerId
        };
    }
}
