using MediatR;

namespace Application.Usecases.Entries.DeleteEntry;

public sealed record DeleteEntryCommand(
    int EntryId,
    int HorseOwnerId
) : IRequest<bool>;