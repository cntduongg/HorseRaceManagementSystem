namespace Application.Usecases.SettlementRuns.GetSettlementRunDetail;

public sealed record SettlementRunDetailResponse(
	int SettlementRunId,
	int RaceId,
	string Type,
	string Status,
	int TotalPredictions,
	decimal TotalBetAmount,
	decimal TotalPayoutAmount,
	int? TriggeredByAdminId
);