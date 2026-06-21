using MediatR;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;

public sealed record GetJockeyInvitationDetailQuery(
    int InvitationId
) : IRequest<JockeyInvitationDetailResponse?>;