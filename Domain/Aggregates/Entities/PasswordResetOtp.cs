namespace Domain.Aggregates.Entities;

public class PasswordResetOtp
{
    public long OtpId { get; set; }

    public int UserId { get; set; }

    public string OtpCodeHash { get; set; } = null!;

    public int FailedAttempts { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}