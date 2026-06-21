namespace Application.Usecases.Spectators.GetSpectatorDetail;

public sealed record SpectatorDetailResponse(
    int UserId,
    DateTime RegisteredAt,
    bool IsActive
);