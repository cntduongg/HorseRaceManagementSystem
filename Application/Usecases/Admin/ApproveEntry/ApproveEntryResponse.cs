namespace Application.Usecases.Admin.ApproveEntry;

public sealed record ApproveEntryResponse(
	int EntryId,
	string Status,
	int? GateNumber
);