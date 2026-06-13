using MediatR;

namespace Application.Usecases.Entries.GetEntryList;

public sealed class GetEntryListQueryHandler
    : IRequestHandler<GetEntryListQuery, List<EntryListItemResponse>>
{
    public Task<List<EntryListItemResponse>> Handle(
        GetEntryListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var entries = new List<EntryListItemResponse>
        {
            new(1, 1, 10, "Pending"),
            new(2, 1, 11, "Approved")
        };

        return Task.FromResult(entries);
    }
}