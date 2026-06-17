namespace Domain.Aggregates.Entities;
public class User
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public string Status { get; set; } = "Active";
    public DateTime? LockedUntil { get; set; }
    // Jockey-only (nullable for other roles)
    public string? LicenseNumber { get; set; }
    public decimal? Weight { get; set; }
    public string? Bio { get; set; }
    public bool IsProfileComplete { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    // Navigation
    public Role Role { get; set; } = null!;
    public ICollection<Horse> OwnedHorses { get; set; } = new List<Horse>();
    public ICollection<JockeyInvitation> SentInvitations { get; set; } = new List<JockeyInvitation>();
    public ICollection<JockeyInvitation> ReceivedInvitations { get; set; } = new List<JockeyInvitation>();
}
