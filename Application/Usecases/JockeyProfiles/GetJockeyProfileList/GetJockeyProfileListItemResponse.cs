namespace Application.Usecases.JockeyProfiles.GetJockeyProfileList;

public sealed record JockeyProfileListItemResponse(
    int UserId,
    string? FullName,
    string? LicenseNumber,
    int TotalRaces,
    int TotalWins
);