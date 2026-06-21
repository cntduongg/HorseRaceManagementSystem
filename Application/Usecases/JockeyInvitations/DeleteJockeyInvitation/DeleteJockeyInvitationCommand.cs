using MediatR;

namespace Application.Usecases.JockeyInvitations.DeleteJockeyInvitation;

public sealed record DeleteJockeyInvitationCommand(
    int InvitationId
) : IRequest<bool>;