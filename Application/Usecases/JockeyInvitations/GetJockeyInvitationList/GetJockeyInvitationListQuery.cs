using MediatR;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationList;

public sealed record GetJockeyInvitationListQuery()
    : IRequest<List<JockeyInvitationListItemResponse>>;