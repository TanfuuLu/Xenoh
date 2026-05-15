namespace Xenoh.Domain.Entities;

public class RevokedToken
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
