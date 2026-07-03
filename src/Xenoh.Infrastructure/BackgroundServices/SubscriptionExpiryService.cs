using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Features.Subscriptions.Commands.CheckSubscriptionExpiry;

namespace Xenoh.Infrastructure.BackgroundServices;

public sealed class SubscriptionExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpiryService> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay so app finishes startup before the first scan.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var result = await mediator.Send(new CheckSubscriptionExpiryCommand(), stoppingToken);
                if (result.RemindedCount > 0 || result.ExpiredCount > 0)
                {
                    logger.LogInformation(
                        "SubscriptionExpiryService: sent {RemindedCount} expiry reminders, expired {ExpiredCount} subscriptions.",
                        result.RemindedCount, result.ExpiredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "SubscriptionExpiryService tick failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
