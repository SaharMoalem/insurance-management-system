using Insurance.Api.Services;
using Insurance.Api.Services.Interfaces;

namespace Insurance.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        return services;
    }
}
