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
        var query = _context.JockeyInvitations.AsNoTracking();

        // Scope theo role: nài thấy lời mời nhận được; chủ ngựa thấy lời mời đã gửi; admin thấy tất cả.
        if (request.CurrentUserId > 0)
        {
            query = request.Role switch
            {
                "JOCKEY" => query.Where(x => x.JockeyId == request.CurrentUserId),
                "HORSE_OWNER" => query.Where(x => x.HorseOwnerId == request.CurrentUserId),
                _ => query
            };
        }

        return await query
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