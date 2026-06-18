using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Spectators.UpdateSpectator;

public sealed class UpdateSpectatorCommandHandler
    : IRequestHandler<UpdateSpectatorCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateSpectatorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateSpectatorCommand request,
        CancellationToken cancellationToken)
    {
        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (spectator is null)
            return false;

        spectator.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}