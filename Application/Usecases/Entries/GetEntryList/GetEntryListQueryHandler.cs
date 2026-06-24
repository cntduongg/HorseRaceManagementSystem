using Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Entries.GetEntryList;

public sealed class GetEntryListQueryHandler
    : IRequestHandler<GetEntryListQuery, List<EntryListItemResponse>>
{
    private readonly IEntryReadService _entryReadService;

    public GetEntryListQueryHandler(IEntryReadService entryReadService)
    {
        _entryReadService = entryReadService;
    }

    public Task<List<EntryListItemResponse>> Handle(
        GetEntryListQuery request,
        CancellationToken cancellationToken)
    {
        return _entryReadService.GetListAsync(cancellationToken);
    }
}