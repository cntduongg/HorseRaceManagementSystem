using Application.Usecases.Entries.GetEntryList;

namespace Application.Common;

public interface IEntryReadService
{
    Task<List<EntryListItemResponse>> GetListAsync(
        CancellationToken cancellationToken);
}