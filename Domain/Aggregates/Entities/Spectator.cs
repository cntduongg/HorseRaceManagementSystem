namespace Domain.Aggregates.Entities;

public class Spectator
{
    public Guid UserId { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public User? User { get; set; }

    public PointWallet? PointWallet { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

    public ICollection<PredictionSettlement> PredictionSettlements { get; set; } = new List<PredictionSettlement>();
}