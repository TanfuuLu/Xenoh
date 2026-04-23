using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Users.Commands.LogBodyweight;

public sealed record LogBodyweightCommand : IRequest<BodyweightLogResponse>
{
    [Required]
    [Range(20, 500, ErrorMessage = "Weight must be between 20 and 500 kg.")]
    public required decimal Weight { get; init; }
}

public sealed record BodyweightLogResponse(
    Guid Id,
    decimal Weight,
    DateOnly Date
);
