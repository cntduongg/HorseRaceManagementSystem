using MediatR;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationList;

public sealed record GetJockeyInvitationListQuery(
    int CurrentUserId = 0,
    string? Role = null)
    : IRequest<List<JockeyInvitationListItemResponse>>;