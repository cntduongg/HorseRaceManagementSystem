namespace Domain.Aggregates.Entities;

public class SettlementRun
{
    public int SettlementRunId { get; set; }

    public int RaceId { get; set; }

    public string Type { get; set; } = "Publish";

    public string Status { get; set; } = "Completed";

    public int TotalPredictions { get; set; }

    public decimal TotalBetAmount { get; set; }

    public decimal TotalPayoutAmount { get; set; }

    public int? TriggeredByAdminId { get; set; }

    public DateTime CreatedAt { get; set; }

    public Race? Race { get; set; }

    public User? TriggeredByAdmin { get; set; }

    public ICollection<PredictionSettlement> PredictionSettlements { get; set; }
        = new List<PredictionSettlement>();
}