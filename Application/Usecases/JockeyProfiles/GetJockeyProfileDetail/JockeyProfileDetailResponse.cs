namespace Application.Usecases.JockeyProfiles.GetJockeyProfileDetail;

public sealed record JockeyProfileDetailResponse(
    int UserId,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio,
    int TotalRaces,
    int TotalWins,
    int TotalTop3,
    int CareerPrizePoints
);