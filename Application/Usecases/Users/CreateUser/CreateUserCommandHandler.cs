using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Usecases.Users.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");  

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
            throw new InvalidOperationException("PasswordHash is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("FullName is required.");

        if (request.RoleId <= 0)
            throw new InvalidOperationException("RoleId is invalid.");
        var exists = await _context.Users
    .AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var licenseExists = await _context.Users
                .AnyAsync(x => x.LicenseNumber == request.LicenseNumber, cancellationToken);

            if (licenseExists)
                throw new InvalidOperationException("LicenseNumber already exists.");
        }
        if (exists)
            throw new InvalidOperationException("Email already exists.");
        var user = new User
        {
            Email = request.Email.Trim(),
            PasswordHash = request.PasswordHash,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber,
            AvatarUrl = request.AvatarUrl,
            RoleId = request.RoleId,
            LicenseNumber = request.LicenseNumber,
            Weight = request.Weight,
            Bio = request.Bio,
            IsActive = true,
            IsProfileComplete = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync(cancellationToken);

        return user.UserId;
    }
}