namespace Application.Usecases.Spectators.GetSpectatorList;

public sealed record SpectatorListItemResponse(
    int UserId,
    DateTime RegisteredAt,
    bool IsActive
);