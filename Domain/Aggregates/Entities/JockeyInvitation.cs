namespace Domain.Aggregates.Entities;
public class JockeyInvitation
{
    public int InvitationId { get; set; }
    public int HorseOwnerId { get; set; }
    public int JockeyId { get; set; }
    public int HorseId { get; set; }
    public int RaceId { get; set; }
    public string Status { get; set; } = "Pending";
    // Pending|Accepted|Declined|Confirmed|Cancelled|Expired
    public string? Message { get; set; }
    public string? ResponseReason { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public User HorseOwner { get; set; } = null!;
    public User Jockey { get; set; } = null!;
    public Horse Horse { get; set; } = null!;
    public Race Race { get; set; } = null!;
}
