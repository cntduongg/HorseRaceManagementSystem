namespace Domain.Aggregates.Entities;
public class PasswordResetOtp
{
    public long OtpId { get; set; }
    public int UserId { get; set; }
    public string OtpCode { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
}
