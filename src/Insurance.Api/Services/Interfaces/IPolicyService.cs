using Insurance.Api.Contracts.Policies;
using Insurance.Api.Domain.Enums;

namespace Insurance.Api.Services.Interfaces;

public interface IPolicyService
{
    Task<PolicyResponse> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PolicyResponse>> GetAllAsync(
        int? customerId = null,
        PolicyType? type = null,
        bool? active = null,
        PolicyStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<PolicyResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PolicyResponse> UpdateAsync(int id, UpdatePolicyRequest request, CancellationToken cancellationToken = default);

    Task<PolicyResponse> CancelAsync(int id, CancellationToken cancellationToken = default);
}
