using MediatR;

namespace Application.Usecases.Admin.GetPendingEntries;

public sealed record GetPendingEntriesQuery()
    : IRequest<List<GetPendingEntriesResponse>>;