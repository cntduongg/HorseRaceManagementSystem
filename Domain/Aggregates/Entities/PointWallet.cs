namespace Domain.Aggregates.Entities;

public class PointWallet
{
    public Guid WalletId { get; set; }

    public int SpectatorId { get; set; }

    public decimal Balance { get; set; } = 100;

    public bool IsFrozen { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Spectator? Spectator { get; set; }

    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}   