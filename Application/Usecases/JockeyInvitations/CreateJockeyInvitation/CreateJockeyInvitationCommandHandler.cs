using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.CreateJockeyInvitation;

public sealed class CreateJockeyInvitationCommandHandler
    : IRequestHandler<CreateJockeyInvitationCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateJockeyInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseOwnerId <= 0)
            throw new InvalidOperationException("HorseOwnerId is required.");

        if (request.JockeyId <= 0)
            throw new InvalidOperationException("JockeyId is required.");

        if (request.HorseId <= 0)
            throw new InvalidOperationException("HorseId is required.");

        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is required.");

        if (request.HorseOwnerId == request.JockeyId)
            throw new InvalidOperationException("Horse owner and jockey must be different.");

        var horseOwnerExists = await _context.Users
            .AnyAsync(x => x.UserId == request.HorseOwnerId, cancellationToken);

        if (!horseOwnerExists)
            throw new InvalidOperationException("Horse owner does not exist.");

        var jockeyExists = await _context.Users
            .AnyAsync(x => x.UserId == request.JockeyId, cancellationToken);

        if (!jockeyExists)
            throw new InvalidOperationException("Jockey does not exist.");

        var horseExists = await _context.Horses
            .AnyAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (!horseExists)
            throw new InvalidOperationException("Horse does not exist.");

        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race does not exist.");

        var exists = await _context.JockeyInvitations
            .AnyAsync(x =>
                x.HorseOwnerId == request.HorseOwnerId &&
                x.JockeyId == request.JockeyId &&
                x.HorseId == request.HorseId &&
                x.RaceId == request.RaceId &&
                x.Status == "Pending",
                cancellationToken);

        if (exists)
            throw new InvalidOperationException("Invitation already exists.");

        var invitation = new JockeyInvitation
        {
            HorseOwnerId = request.HorseOwnerId,
            JockeyId = request.JockeyId,
            HorseId = request.HorseId,
            RaceId = request.RaceId,
            Message = request.Message?.Trim(),
            Status = "Pending",
            SentAt = DateTime.UtcNow
        };

        _context.JockeyInvitations.Add(invitation);

        await _context.SaveChangesAsync(cancellationToken);

        return invitation.InvitationId;
    }
}