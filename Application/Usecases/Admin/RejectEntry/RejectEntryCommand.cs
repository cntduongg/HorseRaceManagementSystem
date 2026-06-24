using MediatR;

namespace Application.Usecases.Admin.RejectEntry;

public sealed record RejectEntryCommand(
    int EntryId,
    string? Reason
) : IRequest<RejectEntryResponse>;