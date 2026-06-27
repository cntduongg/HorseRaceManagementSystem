using MediatR;

namespace Application.Usecases.Admin.RejectEntry;

public sealed record RejectEntryCommand(
    int EntryId,
    int AdminId,
    string? Reason
) : IRequest<RejectEntryResponse>;