namespace Domain.Aggregates.Entities;

public class Prediction
{
    public Guid PredictionId { get; set; }

    public Guid RaceId { get; set; }

    public Guid SpectatorId { get; set; }

    public Guid FirstEntryId { get; set; }

    public Guid SecondEntryId { get; set; }

    public Guid ThirdEntryId { get; set; }

    public decimal BetAmount { get; set; }

    public decimal OddsLocked1 { get; set; }

    public decimal OddsLocked2 { get; set; }

    public decimal OddsLocked3 { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Race? Race { get; set; }

    public Spectator? Spectator { get; set; }

    public Entry? FirstEntry { get; set; }

    public Entry? SecondEntry { get; set; }

    public Entry? ThirdEntry { get; set; }
}