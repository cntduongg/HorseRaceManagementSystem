namespace Domain.Aggregates.Entities;

public class Prediction
{
    public int PredictionId { get; set; }

    public int RaceId { get; set; }

    public int LegNumber { get; set; }

    public int SpectatorId { get; set; }

    public int FirstEntryId { get; set; }

    public int? SecondEntryId { get; set; }

    public int? ThirdEntryId { get; set; }

    public decimal BetAmount { get; set; }

    public decimal OddsLocked1 { get; set; }

    public decimal? OddsLocked2 { get; set; }

    public decimal? OddsLocked3 { get; set; }

    public string Status { get; set; } = PredictionStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Race? Race { get; set; }

    public Leg? Leg { get; set; }
    
    public Spectator? Spectator { get; set; }

    public Entry? FirstEntry { get; set; }

    public Entry? SecondEntry { get; set; }

    public Entry? ThirdEntry { get; set; }
}