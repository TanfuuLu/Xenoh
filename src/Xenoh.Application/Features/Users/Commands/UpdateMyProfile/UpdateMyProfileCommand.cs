using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand : IRequest<UserProfileResponse>
{
    [Range(50, 300, ErrorMessage = "Height must be between 50 and 300 cm.")]
    public decimal? Height { get; init; }

    [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
    public Gender? Gender { get; init; }

    public DateOnly? DateOfBirth { get; init; }
}
