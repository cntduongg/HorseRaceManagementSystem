using MediatR;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationList;

public sealed class GetJockeyInvitationListQueryHandler
    : IRequestHandler<
        GetJockeyInvitationListQuery,
        List<JockeyInvitationListItemResponse>>
{
    public Task<List<JockeyInvitationListItemResponse>> Handle(
        GetJockeyInvitationListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var invitations =
            new List<JockeyInvitationListItemResponse>
            {
                new(
                    1,
                    10,
                    20,
                    "Pending",
                    DateTime.UtcNow
                ),
                new(
                    2,
                    11,
                    21,
                    "Accepted",
                    DateTime.UtcNow
                )
            };

        return Task.FromResult(invitations);
    }
}