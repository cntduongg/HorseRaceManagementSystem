using MediatR;

namespace Application.Usecases.Entries.DeleteEntry;

public sealed record DeleteEntryCommand(int EntryId)
    : IRequest<bool>;