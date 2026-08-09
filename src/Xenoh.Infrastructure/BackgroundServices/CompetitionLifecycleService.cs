using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Features.Competitions;

namespace Xenoh.Infrastructure.BackgroundServices;

/// <summary>
/// Moves competitions along their clock: registration closes when its window ends, and the event
/// closes when its end date passes. Without this an event stays "Published" forever.
/// </summary>
public sealed class CompetitionLifecycleService(
    IServiceScopeFactory scopeFactory,
    ILogger<CompetitionLifecycleService> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

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
                var advanced = await mediator.Send(new AdvanceCompetitionLifecycleCommand(), stoppingToken);
                if (advanced > 0)
                    logger.LogInformation("CompetitionLifecycleService: advanced {Count} competition events.", advanced);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "CompetitionLifecycleService tick failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
