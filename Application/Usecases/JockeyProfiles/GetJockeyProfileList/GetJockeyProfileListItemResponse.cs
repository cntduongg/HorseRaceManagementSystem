namespace Application.Usecases.JockeyProfiles.GetJockeyProfileList;

public sealed record JockeyProfileListItemResponse(
    int UserId,
    string? LicenseNumber,
    int TotalRaces,
    int TotalWins
);