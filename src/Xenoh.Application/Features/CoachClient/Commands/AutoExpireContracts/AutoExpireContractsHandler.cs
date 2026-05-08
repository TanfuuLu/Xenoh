using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.AutoExpireContracts;

public sealed class AutoExpireContractsHandler(
    IApplicationDbContext db,
    INotificationService notificationService
) : IRequestHandler<AutoExpireContractsCommand, int>
{
    public async ValueTask<int> Handle(AutoExpireContractsCommand request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var due = await db.CoachClientRelationships
            .Where(r => r.Status == RelationshipStatus.Active
                     && r.EndDate != null
                     && r.EndDate < today)
            .ToListAsync(cancellationToken);

        foreach (var r in due)
        {
            r.Status = RelationshipStatus.Expired;
            r.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var r in due)
        {
            await notificationService.NotifyAsync(
                r.ClientId,
                "ContractExpired",
                "Hợp đồng huấn luyện đã hết hạn. Bạn có thể gửi yêu cầu gia hạn hoặc kết thúc.",
                r.Id,
                "CoachRequest",
                cancellationToken);

            await notificationService.NotifyAsync(
                r.CoachId,
                "ContractExpired",
                "Hợp đồng huấn luyện đã hết hạn. Đối tác có thể gửi yêu cầu gia hạn.",
                r.Id,
                "CoachRequest",
                cancellationToken);
        }

        return due.Count;
    }
}
