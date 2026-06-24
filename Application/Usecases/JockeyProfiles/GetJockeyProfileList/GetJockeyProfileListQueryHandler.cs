using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyProfiles.GetJockeyProfileList;

public sealed class GetJockeyProfileListQueryHandler
    : IRequestHandler<GetJockeyProfileListQuery, List<JockeyProfileListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetJockeyProfileListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JockeyProfileListItemResponse>> Handle(
        GetJockeyProfileListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.JockeyProfiles
            .Select(x => new JockeyProfileListItemResponse(
                x.UserId,
                x.LicenseNumber,
                x.TotalRaces,
                x.TotalWins
            ))
            .ToListAsync(cancellationToken);
    }
}