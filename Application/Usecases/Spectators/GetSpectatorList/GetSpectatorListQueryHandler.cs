using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Spectators.GetSpectatorList;

public sealed class GetSpectatorListQueryHandler
    : IRequestHandler<GetSpectatorListQuery, List<SpectatorListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetSpectatorListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpectatorListItemResponse>> Handle(
        GetSpectatorListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Spectators
            .Select(x => new SpectatorListItemResponse(
                x.UserId,
                x.RegisteredAt,
                x.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}