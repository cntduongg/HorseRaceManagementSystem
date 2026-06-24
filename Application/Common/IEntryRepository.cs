using Domain.Aggregates.Entities;

namespace Application.Common;

public interface IEntryRepository
{
    /// <summary>
    /// Tracked entries for a horse in a given status — used by Flow 1 to auto-cancel
    /// Pending entries when a horse is revoked.
    /// </summary>
    Task<List<Entry>> GetByHorseAndStatusAsync(
        int horseId,
        string status,
        CancellationToken cancellationToken = default);
}
