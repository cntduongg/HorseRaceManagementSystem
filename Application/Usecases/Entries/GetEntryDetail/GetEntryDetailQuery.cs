using MediatR;

namespace Application.Usecases.Entries.GetEntryDetail;

public sealed record GetEntryDetailQuery(int EntryId)
    : IRequest<EntryDetailResponse?>;