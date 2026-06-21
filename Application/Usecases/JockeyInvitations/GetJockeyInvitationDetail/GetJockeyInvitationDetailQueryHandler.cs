using MediatR;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;

public sealed class GetJockeyInvitationDetailQueryHandler
    : IRequestHandler<
        GetJockeyInvitationDetailQuery,
        JockeyInvitationDetailResponse?>
{
    public Task<JockeyInvitationDetailResponse?> Handle(
        GetJockeyInvitationDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response =
            new JockeyInvitationDetailResponse(
                request.InvitationId,
                1,
                2,
                3,
                4,
                "Pending",
                "Please join this race",
                null,
                DateTime.UtcNow
            );

        return Task.FromResult<
            JockeyInvitationDetailResponse?>(response);
    }
}