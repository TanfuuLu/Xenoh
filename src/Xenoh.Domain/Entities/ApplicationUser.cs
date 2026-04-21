using Microsoft.AspNetCore.Identity;

namespace Xenoh.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Plan> Plans { get; set; } = [];

    // As client
    public CoachClientRelationship? CoachRelationship { get; set; }

    // As coach
    public ICollection<CoachClientRelationship> Clients { get; set; } = [];
}
