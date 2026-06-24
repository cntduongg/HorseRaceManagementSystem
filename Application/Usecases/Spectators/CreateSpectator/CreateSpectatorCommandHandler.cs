using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Spectators.CreateSpectator;

public sealed class CreateSpectatorCommandHandler
    : IRequestHandler<CreateSpectatorCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateSpectatorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateSpectatorCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
            throw new InvalidOperationException("UserId is required.");

        var userExists = await _context.Users
            .AnyAsync(x => x.UserId == request.UserId, cancellationToken);

        if (!userExists)
            throw new InvalidOperationException("User does not exist.");

        var exists = await _context.Spectators
            .AnyAsync(x => x.UserId == request.UserId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Spectator already exists.");

        var spectator = new Spectator
        {
            UserId = request.UserId,
            RegisteredAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Spectators.Add(spectator);
        await _context.SaveChangesAsync(cancellationToken);

        return spectator.UserId;
    }
}