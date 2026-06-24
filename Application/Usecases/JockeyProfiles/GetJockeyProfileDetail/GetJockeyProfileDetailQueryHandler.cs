using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyProfiles.GetJockeyProfileDetail;

public sealed class GetJockeyProfileDetailQueryHandler
    : IRequestHandler<GetJockeyProfileDetailQuery, JockeyProfileDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetJockeyProfileDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JockeyProfileDetailResponse?> Handle(
        GetJockeyProfileDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.JockeyProfiles
            .Where(x => x.UserId == request.UserId)
            .Select(x => new JockeyProfileDetailResponse(
                x.UserId,
                x.User != null ? x.User.FullName : null,
                x.LicenseNumber,
                x.Weight,
                x.Bio,
                x.TotalRaces,
                x.TotalWins,
                x.TotalTop3,
                x.CareerPrizePoints
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}