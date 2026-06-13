using MediatR;

namespace Application.Usecases.Entries.UpdateEntry;

public sealed record UpdateEntryCommand(
    int EntryId,
    string Status,
    int? GateNumber
) : IRequest<bool>;