using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.GetHorseDetail;

public sealed class GetHorseDetailQueryHandler
    : IRequestHandler<GetHorseDetailQuery, HorseDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetHorseDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HorseDetailResponse?> Handle(
    GetHorseDetailQuery request,
    CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0) return null;

        return await _context.Horses
            .Where(x => x.HorseId == request.HorseId)
            .Select(x => new HorseDetailResponse(
                x.HorseId,
                x.OwnerId,
                x.Owner.FullName,
                x.Name,
                x.Breed,
                x.BirthYear,
                x.Color,
                x.Status,
                x.ImageUrl,
                x.CreatedAt,
                x.RejectionReason))
            .FirstOrDefaultAsync(cancellationToken);
    }
}