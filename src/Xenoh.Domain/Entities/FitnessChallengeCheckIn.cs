using System.ComponentModel.DataAnnotations;
using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public sealed class FitnessChallengeCheckIn : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public FitnessChallenge Challenge { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateOnly LocalDate { get; set; }
    [MaxLength(500)]
    public string? Note { get; set; }
}
