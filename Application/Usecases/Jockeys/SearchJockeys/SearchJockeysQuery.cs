using Application.Common;

namespace Application.Usecases.Jockeys.SearchJockeys;

public sealed record SearchJockeysQuery(
    string? Keyword,
    int? MinTotalRaces,
    int? MinWins,
    int? MinTop3,
    int? MinPrizePoints
) : IQuery<List<SearchJockeyResponse>>;