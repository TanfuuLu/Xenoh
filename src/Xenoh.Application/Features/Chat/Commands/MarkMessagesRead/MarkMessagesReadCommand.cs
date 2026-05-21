using Mediator;

namespace Xenoh.Application.Features.Chat.Commands.MarkMessagesRead;

public sealed record MarkMessagesReadCommand(Guid RelationshipId) : IRequest;
