namespace Application.Usecases.Admin.GetPendingEntries;

public sealed record GetPendingEntriesResponse(
    int EntryId,
    int RaceId,
    string RaceName,
    int HorseId,
    string HorseName,
    int JockeyId,
    string JockeyName,
    DateTime SubmittedAt
);