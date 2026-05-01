namespace Xenoh.Domain.Entities;

public class ExternalAuthTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TicketHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
