namespace Application.Usecases.Admin.GetReviewHistory;

using System.Text.Json;

public sealed record ReviewHistoryResponse(
	long Id,
	string Entity,
	int EntityId,
	string Action,
	string? Reason,
	JsonElement? BeforeData,
	JsonElement? AfterData,
	int AdminId,
	string AdminName,
	DateTime CreatedAt
);
