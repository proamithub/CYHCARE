using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Infrastructure.Data;

namespace CYHCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        return services;
    }
}
