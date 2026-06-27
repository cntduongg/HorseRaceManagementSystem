namespace Application.Usecases.Admin.GetReviewHistory;

public sealed record ReviewHistoryResponse(
	long Id,
	string Entity,
	int EntityId,
	string Action,
	string? Reason,
	int AdminId,
	string AdminName,
	DateTime CreatedAt
);