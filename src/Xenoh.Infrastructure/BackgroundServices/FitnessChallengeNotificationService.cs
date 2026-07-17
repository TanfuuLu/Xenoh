using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Features.FitnessChallenges;

namespace Xenoh.Infrastructure.BackgroundServices;

public sealed class FitnessChallengeNotificationService(
    IServiceScopeFactory scopeFactory,
    ILogger<FitnessChallengeNotificationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var count = await scope.ServiceProvider.GetRequiredService<IMediator>()
                    .Send(new ProcessFitnessChallengeNotificationsCommand(), stoppingToken);
                if (count > 0) logger.LogInformation("Sent {Count} fitness challenge notifications.", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Fitness challenge notification scan failed."); }
            try { await Task.Delay(TimeSpan.FromHours(6), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
