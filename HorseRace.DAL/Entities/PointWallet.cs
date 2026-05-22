namespace HorseRace.DAL.Entities;
public class PointWallet
{
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public int Balance { get; set; } = 100;
    public bool IsFrozen { get; set; } = false;
    public uint RowVersion { get; set; } // PostgreSQL xmin optimistic concurrency
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
