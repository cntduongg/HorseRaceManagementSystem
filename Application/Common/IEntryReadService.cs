using Application.Usecases.Entries.GetEntryList;

namespace Application.Common;

public interface IEntryReadService
{
    // ownerId != null → chỉ entry của owner đó (Flow 2: owner thấy của mình).
    // raceId != null → lọc theo cuộc đua.
    Task<List<EntryListItemResponse>> GetListAsync(
        int? ownerId,
        int? raceId,
        CancellationToken cancellationToken);
}