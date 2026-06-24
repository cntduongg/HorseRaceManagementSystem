namespace Application.Usecases.Admin.RejectEntry;

public sealed record RejectEntryResponse(
    int EntryId,
    string Status,
    string? Reason
);