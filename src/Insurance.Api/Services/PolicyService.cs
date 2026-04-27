using Insurance.Api.Contracts.Policies;
using Insurance.Api.Data;
using Insurance.Api.Domain.Entities;
using Insurance.Api.Domain.Enums;
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
        await EnsureNoDuplicateActivePolicyTypeAsync(request.CustomerId, request.Type, null, cancellationToken);

        var policy = new Policy
        {
            PolicyNumber = request.PolicyNumber.Trim(),
            Type = request.Type,
            Status = PolicyStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            CustomerId = request.CustomerId
        };

        _dbContext.Policies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetAllAsync(
        int? customerId = null,
        PolicyType? type = null,
        bool? active = null,
        PolicyStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Policies
            .AsNoTracking()
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (active.HasValue)
        {
            if (active.Value)
            {
                query = query.Where(x => x.Status == PolicyStatus.Active);
            }
            else
            {
                query = query.Where(x => x.Status != PolicyStatus.Active);
            }
        }

        return await query
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
            throw new NotFoundException("policy_not_found", $"Policy with id '{id}' was not found.");
        }

        return Map(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(int id, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.Policies
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException("policy_not_found", $"Policy with id '{id}' was not found.");
        }

        await EnsureCustomerExistsAsync(request.CustomerId, cancellationToken);
        await EnsurePolicyNumberUniqueAsync(request.PolicyNumber, id, cancellationToken);
        ValidatePolicyDatesAndPremium(request.StartDate, request.EndDate, request.PremiumAmount);
        if (policy.Status == PolicyStatus.Active)
        {
            await EnsureNoDuplicateActivePolicyTypeAsync(request.CustomerId, request.Type, id, cancellationToken);
        }

        policy.PolicyNumber = request.PolicyNumber.Trim();
        policy.Type = request.Type;
        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.PremiumAmount = request.PremiumAmount;
        policy.CustomerId = request.CustomerId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyResponse> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.Policies
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException("policy_not_found", $"Policy with id '{id}' was not found.");
        }

        if (policy.Status != PolicyStatus.Active)
        {
            throw new ConflictException(
                "invalid_policy_status_transition",
                $"Policy with id '{id}' cannot be cancelled from status '{policy.Status}'.");
        }

        policy.Status = PolicyStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    private async Task EnsureCustomerExistsAsync(int customerId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Customers.AnyAsync(x => x.Id == customerId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("customer_not_found", $"Customer with id '{customerId}' was not found.");
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
            throw new ConflictException(
                "duplicate_policy_number",
                $"A policy with number '{normalizedPolicyNumber}' already exists.");
        }
    }

    private async Task EnsureNoDuplicateActivePolicyTypeAsync(
        int customerId,
        PolicyType type,
        int? existingPolicyId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Policies
            .Where(x => x.CustomerId == customerId && x.Type == type && x.Status == PolicyStatus.Active);

        if (existingPolicyId.HasValue)
        {
            query = query.Where(x => x.Id != existingPolicyId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);
        if (exists)
        {
            throw new ConflictException(
                "duplicate_active_policy_type",
                $"Customer '{customerId}' already has an active policy of type '{type}'.");
        }
    }

    private static void ValidatePolicyDatesAndPremium(DateOnly startDate, DateOnly endDate, decimal premiumAmount)
    {
        if (startDate >= endDate)
        {
            throw new ValidationException("invalid_policy_date_range", "Policy start date must be earlier than end date.");
        }

        if (premiumAmount <= 0)
        {
            throw new ValidationException("invalid_premium_amount", "Policy premium amount must be greater than zero.");
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
