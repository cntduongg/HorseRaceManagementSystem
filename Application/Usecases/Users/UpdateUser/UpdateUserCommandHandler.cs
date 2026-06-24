using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Users.UpdateUser;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("FullName is required.");

        if (request.RoleId <= 0)
            throw new InvalidOperationException("RoleId is invalid.");

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId,
                cancellationToken);

        if (user is null)
            return false;

        user.Email = request.Email.Trim();
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.LockedUntil = request.LockedUntil;
        user.LicenseNumber = request.LicenseNumber;
        user.Weight = request.Weight;
        user.Bio = request.Bio;
        user.IsProfileComplete = request.IsProfileComplete;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}