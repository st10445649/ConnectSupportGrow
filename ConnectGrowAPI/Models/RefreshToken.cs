using ConnectGrowAPI.Models;


// https://towardsdev.com/understanding-refresh-tokens-in-web-api-development-with-c-17761a591cfc

public class RefreshToken
{
    public int Id { get; set; }
 
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
 
    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
 
    public string? CreatedByIp { get; set; }
 
    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;
 
    public bool IsActive(DateTime utcNow) => RevokedAt is null && !IsExpired(utcNow);
}
 