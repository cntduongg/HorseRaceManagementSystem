using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed class GetRaceResultListQueryHandler
	: IRequestHandler<GetRaceResultListQuery, List<RaceResultListItemResponse>>
{
	private readonly IApplicationDbContext _context;

	public GetRaceResultListQueryHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<List<RaceResultListItemResponse>> Handle(
		GetRaceResultListQuery request,
		CancellationToken cancellationToken)
	{
		return await _context.RaceResults
            .Select(x => new RaceResultListItemResponse(
    x.RaceId,
    x.EntryId,
    x.TotalPoints,
    x.FinalPosition,
    x.IsRaceDQ,
    x.LegWinCount,
    x.LegTop3Count
))
            .ToListAsync(cancellationToken);
	}
}