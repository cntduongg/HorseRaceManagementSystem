namespace Application.Usecases.Admin.ResultPublication;

public sealed record ReviewRacePublicationResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    bool CanPublish,
    List<string> Warnings,
    List<FinalStandingReviewItem> FinalStandings,
    PredictionSettlementPreview PredictionPreview);

public sealed record FinalStandingReviewItem(
    int EntryId,
    int HorseId,
    string? HorseName,
    int JockeyId,
    string? JockeyName,
    int HorseOwnerId,
    string? HorseOwnerName,
    int? FinalPosition,
    bool IsRaceDQ,
    string? ViolationNote);

public sealed record PredictionSettlementPreview(
    int TotalPredictions,
    int CorrectPredictions,
    int WrongPredictions,
    decimal TotalBetAmount,
    decimal EstimatedTotalPayout);