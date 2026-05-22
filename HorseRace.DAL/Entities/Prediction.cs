namespace HorseRace.DAL.Entities;
public class Prediction
{
    public long PredictionId { get; set; }
    public int RaceId { get; set; }
    public int UserId { get; set; }
    public int BetAmount { get; set; }
    public int FirstEntryId { get; set; }
    public int SecondEntryId { get; set; }
    public int ThirdEntryId { get; set; }
    public decimal OddsLocked1 { get; set; }
    public decimal OddsLocked2 { get; set; }
    public decimal OddsLocked3 { get; set; }
    public string Status { get; set; } = "Pending";
    // Pending|Won_Correct|Won_Partial2|Won_Partial1|Lost|Cancelled|Refunded
    public int? Payout { get; set; }
    public int? CorrectMatches { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Race Race { get; set; } = null!;
    public User User { get; set; } = null!;
    public Entry FirstEntry { get; set; } = null!;
    public Entry SecondEntry { get; set; } = null!;
    public Entry ThirdEntry { get; set; } = null!;
}
