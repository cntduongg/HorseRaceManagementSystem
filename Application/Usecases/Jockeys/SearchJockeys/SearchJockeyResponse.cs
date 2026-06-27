namespace Application.Usecases.Jockeys.SearchJockeys;

public sealed record SearchJockeyResponse(
    int UserId,
    string FullName,
    string? AvatarUrl,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio,

    int TotalRaces,
    int TotalWins,
    int TotalTop3,
    int CareerPrizePoints
);