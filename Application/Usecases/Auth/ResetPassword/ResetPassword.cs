using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Auth.ResetPassword;

// POST /api/auth/reset-password — xác thực OTP & đặt mật khẩu mới.
public sealed record ResetPasswordCommand(
    string Email,
    string OtpCode,
    string NewPassword,
    string? ConfirmPassword) : IRequest<ResetPasswordResponse>;

public sealed record ResetPasswordResponse(bool Success, string Message);

public sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<ResetPasswordResponse> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.OtpCode))
            throw new InvalidOperationException("OTP code is required.");
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new InvalidOperationException("New password must be at least 8 characters.");
        if (request.ConfirmPassword is not null && request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("Password confirmation does not match.");

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            ?? throw new InvalidOperationException("Invalid OTP code or email.");

        var now = DateTime.UtcNow;
        var otp = await _context.PasswordResetOtps
            .Where(o => o.UserId == user.UserId &&
                        o.OtpCode == request.OtpCode.Trim() &&
                        o.UsedAt == null &&
                        o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The OTP code is invalid or has expired.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = now;
        otp.UsedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse(true, "Password reset successfully.");
    }
}
