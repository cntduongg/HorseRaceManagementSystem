using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Microsoft.EntityFrameworkCore;
using MediatR;
namespace Application.Usecases.Admin.GetPendingHorses;

public class GetPendingHorsesQueryHandler
	: IRequestHandler<GetPendingHorsesQuery, List<GetPendingHorsesResponse>>
{
	private readonly IApplicationDbContext _context;

	public GetPendingHorsesQueryHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<List<GetPendingHorsesResponse>> Handle(
		GetPendingHorsesQuery request,
		CancellationToken cancellationToken)
	{
        return await _context.Horses
    .Where(h => h.Status == HorseStatus.Pending)
    .Include(h => h.Owner)
    .OrderBy(h => h.CreatedAt)
    .Select(h => new GetPendingHorsesResponse(
        h.HorseId,
        h.Name,
        h.Breed,
        h.Color,
        h.BirthYear,
        h.Status,
        h.OwnerId,
        h.Owner.FullName,
        h.CreatedAt,
        h.ImageUrl
    ))
    .ToListAsync(cancellationToken);
    }
}