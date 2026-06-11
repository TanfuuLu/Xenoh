using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mediator;

namespace Xenoh.Application.Features.Nutrition.Commands.UpdateNutritionDailyLog;

public sealed record UpdateNutritionDailyLogCommand : IRequest<NutritionDailyLogResponse>
{
    [JsonIgnore]
    public DateOnly Date { get; init; }

    [Range(0, 20000)]
    public int Calories { get; init; }

    [Range(0, 2000)]
    public decimal ProteinG { get; init; }

    [Range(0, 3000)]
    public decimal CarbsG { get; init; }

    [Range(0, 2000)]
    public decimal FatG { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
