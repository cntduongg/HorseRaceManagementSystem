using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Users.ChangePassword;

// PUT /api/users/{id}/change-password — đổi mật khẩu (UserId resolve từ JWT ở controller).
public sealed record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<ChangePasswordResponse>;

public sealed record ChangePasswordResponse(bool Success, string Message);

public sealed class ChangePasswordCommandHandler
    : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResponse> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new InvalidOperationException("Mật khẩu hiện tại là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 8 ký tự.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng.");

        if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("Mật khẩu mới phải khác mật khẩu cũ.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResponse(true, "Đổi mật khẩu thành công.");
    }
}
