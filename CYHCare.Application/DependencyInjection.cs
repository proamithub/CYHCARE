using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.Services;

namespace CYHCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IHelperService, HelperService>();

        // Default fallback dispatcher (overridden by SignalR in API layer)
        services.TryAddScoped<INotificationDispatcher, NullNotificationDispatcher>();

        return services;
    }
}
