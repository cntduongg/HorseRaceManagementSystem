namespace Application.Usecases.Predictions.PlacePrediction;

public sealed class PlacePredictionRequest
{
    public int EntryId { get; set; }

    public decimal BetAmount { get; set; }
}