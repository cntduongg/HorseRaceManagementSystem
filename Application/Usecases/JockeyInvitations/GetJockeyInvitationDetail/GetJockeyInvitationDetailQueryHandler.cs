using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;

public sealed class GetJockeyInvitationDetailQueryHandler
    : IRequestHandler<GetJockeyInvitationDetailQuery, JockeyInvitationDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetJockeyInvitationDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JockeyInvitationDetailResponse?> Handle(
        GetJockeyInvitationDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.JockeyInvitations
            .Where(x => x.InvitationId == request.InvitationId)
            .Select(x => new JockeyInvitationDetailResponse(
                x.InvitationId,
                x.HorseOwnerId,
                x.JockeyId,
                x.HorseId,
                x.RaceId,
                x.Status,
                x.Message,
                x.ResponseReason,
                x.SentAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}