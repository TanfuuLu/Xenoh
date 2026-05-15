using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient.Commands.AutoExpireContracts;

namespace Xenoh.Infrastructure.BackgroundServices;

public sealed class ContractExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<ContractExpiryService> logger
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
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var expired = await mediator.Send(new AutoExpireContractsCommand(), stoppingToken);
                if (expired > 0)
                    logger.LogInformation("ContractExpiryService: marked {Count} relationships as Expired.", expired);

                var deletedTokens = await db.RevokedTokens
                    .Where(t => t.ExpiresAt < DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);
                if (deletedTokens > 0)
                    logger.LogInformation("ContractExpiryService: purged {Count} expired revoked tokens.", deletedTokens);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "ContractExpiryService tick failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
