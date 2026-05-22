namespace HorseRace.DAL.Entities;
// APPEND-ONLY — never UPDATE or DELETE
public class WalletTransaction
{
    public long WalletTransactionId { get; set; }
    public int WalletId { get; set; }
    public string TransactionType { get; set; } = null!;
    // Initial|WeeklyBonus|AdminAdjustment|BetPlaced|BetWon|BetRefund|BetCancelled
    public int Amount { get; set; } // signed (+/-)
    public int BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Description { get; set; }
    public int? PerformedBy { get; set; } // null = system job
    public DateTime CreatedAt { get; set; }
    public PointWallet Wallet { get; set; } = null!;
}
