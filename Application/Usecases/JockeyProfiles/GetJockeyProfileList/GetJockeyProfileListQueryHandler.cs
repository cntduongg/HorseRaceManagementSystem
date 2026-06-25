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
        // Flow 2 — Owner chỉ thấy nài có hồ sơ đủ (License Number + Weight).
        return await _context.JockeyProfiles
      .Where(x => x.LicenseNumber != null && x.LicenseNumber != ""
                  && x.Weight != null && x.Weight > 0)
      .OrderByDescending(x => x.TotalWins)
      .Select(x => new JockeyProfileListItemResponse(
          x.UserId,
          x.User!.FullName,
          x.LicenseNumber,
          x.TotalRaces,
          x.TotalWins
      ))
      .ToListAsync(cancellationToken);
    }
}