using MediatR;

namespace Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;

public sealed record UpdateJockeyInvitationCommand(
    int InvitationId,
    string Status,
    string? ResponseReason,
    int CurrentUserId = 0
) : IRequest<bool>;