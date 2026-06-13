namespace Application.Usecases.SettlementRuns.GetSettlementRunList;

public sealed record SettlementRunListItemResponse(
    int SettlementRunId,
    int RaceId,
    string Type,
    string Status
);