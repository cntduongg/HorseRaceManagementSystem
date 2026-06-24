using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationList;

public sealed class GetJockeyInvitationListQueryHandler
    : IRequestHandler<GetJockeyInvitationListQuery, List<JockeyInvitationListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetJockeyInvitationListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JockeyInvitationListItemResponse>> Handle(
        GetJockeyInvitationListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.JockeyInvitations
            .AsNoTracking()
            .OrderByDescending(x => x.SentAt)
            .Select(x => new JockeyInvitationListItemResponse(
                x.InvitationId,

                x.HorseOwnerId,
                x.HorseOwner.FullName,

                x.JockeyId,
                x.Jockey.FullName,

                x.RaceId,
                x.Race.Name,

                x.HorseId,
                x.Horse.Name,

                x.Status,
                x.SentAt
            ))
            .ToListAsync(cancellationToken);
    }
}