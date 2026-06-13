namespace Domain.Aggregates.Entities;

public class WalletTransaction
{
    public int WalletTransactionId { get; set; }

    public int WalletId { get; set; }

    public int SpectatorId { get; set; }

    public int? PredictionId { get; set; }

    public int? SettlementRunId { get; set; }

    public int? AdminId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public string? Reason { get; set; }

    public int? RollbackOfTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public PointWallet? Wallet { get; set; }

    public Spectator? Spectator { get; set; }

    public Prediction? Prediction { get; set; }

    public SettlementRun? SettlementRun { get; set; }

    public User? Admin { get; set; }

    public WalletTransaction? RollbackOfTransaction { get; set; }

    public ICollection<WalletTransaction> RollbackTransactions { get; set; }
        = new List<WalletTransaction>();
}