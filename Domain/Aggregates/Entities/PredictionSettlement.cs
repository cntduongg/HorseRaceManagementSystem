namespace Domain.Aggregates.Entities;

public class PredictionSettlement
{
    public Guid PredictionSettlementId { get; set; }

    public Guid SettlementRunId { get; set; }

    public Guid PredictionId { get; set; }

    public int RaceId { get; set; }

    public int SpectatorId { get; set; }

    public int MatchedCount { get; set; }

    public string Outcome { get; set; } = "Lost";

    public decimal BetAmount { get; set; }

    public decimal OddsAverage { get; set; }

    public decimal PayoutAmount { get; set; }

    public decimal NetAmount { get; set; }

    public Guid? PayoutTransactionId { get; set; }

    public Guid? RollbackOfSettlementId { get; set; }

    public bool IsRollbacked { get; set; }

    public DateTime SettledAt { get; set; }

    public DateTime? RollbackAt { get; set; }

    public SettlementRun? SettlementRun { get; set; }

    public Prediction? Prediction { get; set; }

    public Race? Race { get; set; }

    public Spectator? Spectator { get; set; }

    public WalletTransaction? PayoutTransaction { get; set; }

    public PredictionSettlement? RollbackOfSettlement { get; set; }

    public ICollection<PredictionSettlement> RollbackSettlements { get; set; } = new List<PredictionSettlement>();
}