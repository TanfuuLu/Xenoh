using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Coaches;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand : IRequest<UserProfileResponse>
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters.")]
    public string? FirstName { get; init; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters.")]
    public string? LastName { get; init; }

    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
    public string? Bio { get; init; }

    [Range(50, 300, ErrorMessage = "Height must be between 50 and 300 cm.")]
    public decimal? Height { get; init; }

    [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
    public Gender? Gender { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    public CoachMarketplaceProfileDto? CoachMarketplaceProfile { get; init; }

    [StringLength(300, ErrorMessage = "Facebook URL cannot exceed 300 characters.")]
    [Url(ErrorMessage = "Facebook URL must be a valid URL.")]
    public string? FacebookUrl { get; init; }

    [StringLength(300, ErrorMessage = "Instagram URL cannot exceed 300 characters.")]
    [Url(ErrorMessage = "Instagram URL must be a valid URL.")]
    public string? InstagramUrl { get; init; }

    [StringLength(300, ErrorMessage = "Zalo URL cannot exceed 300 characters.")]
    [Url(ErrorMessage = "Zalo URL must be a valid URL.")]
    public string? ZaloUrl { get; init; }
}
