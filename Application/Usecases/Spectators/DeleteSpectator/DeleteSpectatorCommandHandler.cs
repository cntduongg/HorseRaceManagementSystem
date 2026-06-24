using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Spectators.DeleteSpectator;

public sealed class DeleteSpectatorCommandHandler
    : IRequestHandler<DeleteSpectatorCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteSpectatorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteSpectatorCommand request,
        CancellationToken cancellationToken)
    {
        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (spectator is null)
            return false;

        _context.Spectators.Remove(spectator);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}