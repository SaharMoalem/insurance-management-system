using Insurance.Api.Contracts.Policies;

namespace Insurance.Api.Services.Interfaces;

public interface IPolicyService
{
    Task<PolicyResponse> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PolicyResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PolicyResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PolicyResponse> UpdateAsync(int id, UpdatePolicyRequest request, CancellationToken cancellationToken = default);
}
