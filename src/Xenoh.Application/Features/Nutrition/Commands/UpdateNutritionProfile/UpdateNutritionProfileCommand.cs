using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionProfile;

public sealed record UpdateNutritionProfileCommand : IRequest<NutritionProfileResponse>
{
    [JsonIgnore]
    public Guid? UserId { get; init; }

    [Required]
    public ActivityLevel ActivityLevel { get; init; }

    [Required]
    public NutritionGoal Goal { get; init; }

    [Range(20, 400)]
    public decimal? TargetWeightKg { get; init; }

    [Range(800, 8000)]
    public int? CustomCalorieTarget { get; init; }

    [Range(0.5, 4.0)]
    public decimal? ProteinPerKg { get; init; }

    [Range(0.2, 2.0)]
    public decimal? FatPerKg { get; init; }
}
