using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // User Stats (Wave 2)
    public decimal? Height { get; set; }        // cm
    public Gender? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(500)]
    public string? Bio { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Plan> Plans { get; set; } = [];

    // As client
    public CoachClientRelationship? CoachRelationship { get; set; }

    // As coach
    public ICollection<CoachClientRelationship> Clients { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];
}
