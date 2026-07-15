using Application.Common.Interfaces;

namespace Application.Common;

/// <inheritdoc cref="IRaceLiveChangeTracker"/>
public sealed class RaceLiveChangeTracker : IRaceLiveChangeTracker
{
    private readonly HashSet<int> _changedRaceIds = new();

    public void MarkChanged(int raceId) => _changedRaceIds.Add(raceId);

    public IReadOnlyCollection<int> Drain()
    {
        if (_changedRaceIds.Count == 0)
            return Array.Empty<int>();

        var drained = _changedRaceIds.ToArray();
        _changedRaceIds.Clear();
        return drained;
    }
}
