namespace Application.Usecases.Legs.GetLegList;

public sealed record LegListItemResponse(
    int RaceId,
    int LegNumber,
    string Status
);