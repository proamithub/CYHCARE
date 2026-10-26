using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Api.BackgroundServices;

/// <summary>
/// Background service enforcing data retention privacy policies by purging old GPS telemetry logs.
/// </summary>
public class GpsRetentionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GpsRetentionBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
    private const int RetentionDays = 30;

    public GpsRetentionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<GpsRetentionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GpsRetentionBackgroundService started. Retention policy: {Days} days.", RetentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeOldLogsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during scheduled GPS telemetry purge.");
            }

            // Wait for 24 hours or until cancellation is requested
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task PurgeOldLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var gpsRepo = scope.ServiceProvider.GetRequiredService<IGpsRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditRepository>();

        _logger.LogInformation("Executing scheduled sp_Gps_PurgeOld (older than {Days} days)...", RetentionDays);

        var deletedCount = await gpsRepo.PurgeOldAsync(RetentionDays);

        _logger.LogInformation("GPS Purge complete: {Count} old telemetry rows removed.", deletedCount);

        if (deletedCount > 0)
        {
            await auditRepo.InsertAsync(null, "GPS_TELEMETRY_PURGED", 
                $"Data retention purge removed {deletedCount} GPS log entries older than {RetentionDays} days.");
        }
    }
}
