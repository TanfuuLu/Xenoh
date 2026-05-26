using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Chat.Dtos;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Chat.Commands.SendMessage;

public sealed class SendMessageHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IChatRealtimeService chatRealtimeService
) : IRequestHandler<SendMessageCommand, MessageResponse>
{
    public async ValueTask<MessageResponse> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = currentUser.UserId;

        var relationship = await db.CoachClientRelationships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == request.RelationshipId
                     && (r.ClientId == senderId || r.CoachId == senderId)
                     && r.Status == RelationshipStatus.Active,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Relationship not found or you are not a participant.");

        var sender = await db.ApplicationUsers
            .AsNoTracking()
            .FirstAsync(u => u.Id == senderId, cancellationToken);

        var message = new Message
        {
            RelationshipId = request.RelationshipId,
            SenderId = senderId,
            Content = request.Content,
            IsRead = false,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var senderName = $"{sender.FirstName} {sender.LastName}".Trim();

        var response = new MessageResponse(
            message.Id,
            message.RelationshipId,
            message.SenderId,
            senderName,
            message.Content,
            message.Kind.ToString(),
            message.IsRead,
            message.CreatedAt);

        var recipientId = senderId == relationship.ClientId
            ? relationship.CoachId
            : relationship.ClientId;

        await chatRealtimeService.MessageSentAsync(
            request.RelationshipId,
            response,
            [senderId, recipientId],
            cancellationToken);

        return response;
    }
}
