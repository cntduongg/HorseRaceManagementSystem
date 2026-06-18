using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Spectators.GetSpectatorDetail;

public sealed class GetSpectatorDetailQueryHandler
    : IRequestHandler<GetSpectatorDetailQuery, SpectatorDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetSpectatorDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SpectatorDetailResponse?> Handle(
        GetSpectatorDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Spectators
            .Where(x => x.UserId == request.UserId)
            .Select(x => new SpectatorDetailResponse(
                x.UserId,
                x.RegisteredAt,
                x.IsActive
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}