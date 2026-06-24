using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.GetHorseList;

public sealed class GetHorseListQueryHandler
    : IRequestHandler<GetHorseListQuery, List<HorseListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetHorseListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HorseListItemResponse>> Handle(
        GetHorseListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Horses
            .Select(x => new HorseListItemResponse(
                x.HorseId,
                x.Name,
                x.Status,
                x.Breed
            ))
            .ToListAsync(cancellationToken);
    }
}