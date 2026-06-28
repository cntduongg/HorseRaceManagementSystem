namespace Application.DTOs;

public sealed class PredictionRequest
{
    public int EntryId { get; set; }

    public decimal BetAmount { get; set; }
}