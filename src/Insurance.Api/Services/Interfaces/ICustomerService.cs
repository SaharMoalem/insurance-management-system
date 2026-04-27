using Insurance.Api.Contracts.Customers;

namespace Insurance.Api.Services.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CustomerResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
