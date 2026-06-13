using MediatR;

namespace Application.Usecases.JockeyInvitations.CreateJockeyInvitation;

public sealed record CreateJockeyInvitationCommand(
    int HorseOwnerId,
    int JockeyId,
    int HorseId,
    int RaceId,
    string? Message
) : IRequest<int>;