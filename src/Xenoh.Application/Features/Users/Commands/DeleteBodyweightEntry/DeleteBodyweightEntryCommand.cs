using Mediator;

namespace Xenoh.Application.Features.Users.Commands.DeleteBodyweightEntry;

public sealed record DeleteBodyweightEntryCommand(Guid Id) : IRequest;
