using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Infrastructure.Data;
using CYHCare.Infrastructure.Repositories;

namespace CYHCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Connection Factory
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        // Dapper Repositories (One per entity, 100% Stored Procedures)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHelperRepository, HelperRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IGpsRepository, GpsRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
