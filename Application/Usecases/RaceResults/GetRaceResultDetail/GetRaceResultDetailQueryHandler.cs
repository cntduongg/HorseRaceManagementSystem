using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceResults.GetRaceResultDetail;

public sealed class GetRaceResultDetailQueryHandler
    : IRequestHandler<GetRaceResultDetailQuery, RaceResultDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetRaceResultDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RaceResultDetailResponse?> Handle(
        GetRaceResultDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.RaceResults
      .AsNoTracking()
      .Where(x => x.RaceId == request.RaceId && x.EntryId == request.EntryId)
      .Select(x => new RaceResultDetailResponse(
          x.RaceId,
          x.EntryId,
          x.Entry.Horse.Name,
          x.Entry.HorseOwner.FullName,
          x.Entry.Jockey.FullName,
          x.TotalPoints,
          x.FinalPosition,
          x.IsRaceDQ,
          x.LegWinCount,
          x.LegTop3Count,
          x.CreatedAt,
          x.UpdatedAt
      ))
      .FirstOrDefaultAsync(cancellationToken);
    }
}