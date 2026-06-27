using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Jockeys.SearchJockeys;

public sealed class SearchJockeysQueryHandler
    : IRequestHandler<SearchJockeysQuery, List<SearchJockeyResponse>>
{
    private readonly IApplicationDbContext _context;

    public SearchJockeysQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SearchJockeyResponse>> Handle(
        SearchJockeysQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.JockeyProfiles
            .Include(x => x.User)
            .Where(x =>
                x.User != null &&
                x.LicenseNumber != null &&
                x.LicenseNumber != "" &&
                x.Weight != null &&
                x.Weight > 0);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();

            query = query.Where(x =>
                x.User!.FullName.ToLower().Contains(keyword));
        }

        if (request.MinTotalRaces.HasValue)
        {
            query = query.Where(x =>
                x.TotalRaces >= request.MinTotalRaces.Value);
        }

        if (request.MinWins.HasValue)
        {
            query = query.Where(x =>
                x.TotalWins >= request.MinWins.Value);
        }

        if (request.MinTop3.HasValue)
        {
            query = query.Where(x =>
                x.TotalTop3 >= request.MinTop3.Value);
        }

        if (request.MinPrizePoints.HasValue)
        {
            query = query.Where(x =>
                x.CareerPrizePoints >= request.MinPrizePoints.Value);
        }

        return await query
            .Select(x => new SearchJockeyResponse(
                x.UserId,
                x.User!.FullName,
                x.User.AvatarUrl,
                x.LicenseNumber,
                x.Weight,
                x.Bio,

                x.TotalRaces,
                x.TotalWins,
                x.TotalTop3,
                x.CareerPrizePoints
            ))
            .ToListAsync(cancellationToken);
    }
}