using Application.Common;
using Application.Common.Interfaces;
using Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Usecases.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string OtpCode,
    string NewPassword,
    string? ConfirmPassword)
    : IRequest<ResetPasswordResponse>;

public sealed record ResetPasswordResponse(
    bool Success,
    string Message);

public sealed class ResetPasswordCommandHandler
    : IRequestHandler<
        ResetPasswordCommand,
        ResetPasswordResponse>
{
    private const string InvalidOtpMessage =
        "The OTP code is invalid or has expired.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetOtpProtector _otpProtector;
    private readonly PasswordResetOptions _options;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IPasswordResetOtpProtector otpProtector,
        IOptions<PasswordResetOptions> options)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _otpProtector = otpProtector;
        _options = options.Value;
    }

    public async Task<ResetPasswordResponse> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email?
            .Trim()
            .ToLowerInvariant();

        var otpCode = request.OtpCode?.Trim();

        ValidateRequest(
            email,
            otpCode,
            request.NewPassword,
            request.ConfirmPassword);

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Email == email,
                cancellationToken)
            ?? throw new InvalidOperationException(
                InvalidOtpMessage);

        var now = DateTime.UtcNow;

        var otp = await _context.PasswordResetOtps
            .Where(o =>
                o.UserId == user.UserId &&
                o.UsedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                InvalidOtpMessage);

        if (otp.ExpiresAt <= now)
        {
            otp.UsedAt = now;

            await SaveOtpStateAsync(
                cancellationToken);

            throw new InvalidOperationException(
                InvalidOtpMessage);
        }

        if (otp.FailedAttempts >=
            _options.MaxFailedAttempts)
        {
            otp.UsedAt = now;

            await SaveOtpStateAsync(
                cancellationToken);

            throw new InvalidOperationException(
                InvalidOtpMessage);
        }

        if (!_otpProtector.Verify(
                otpCode!,
                otp.OtpCodeHash))
        {
            otp.FailedAttempts++;

            if (otp.FailedAttempts >=
                _options.MaxFailedAttempts)
            {
                otp.UsedAt = now;
            }

            await SaveOtpStateAsync(
                cancellationToken);

            throw new InvalidOperationException(
                InvalidOtpMessage);
        }

        // Vẫn dùng PasswordHasher BCrypt hiện tại.
        user.PasswordHash =
            _passwordHasher.Hash(request.NewPassword);

        user.UpdatedAt = now;

        // Reset thành công thì vô hiệu hóa toàn bộ OTP còn lại.
        var unusedOtps = await _context.PasswordResetOtps
            .Where(o =>
                o.UserId == user.UserId &&
                o.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var unusedOtp in unusedOtps)
        {
            unusedOtp.UsedAt = now;
        }

        try
        {
            await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                InvalidOtpMessage);
        }

        return new ResetPasswordResponse(
            true,
            "Password reset successfully.");
    }

    private async Task SaveOtpStateAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                InvalidOtpMessage);
        }
    }

    private static void ValidateRequest(
        string? email,
        string? otpCode,
        string newPassword,
        string? confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(otpCode) ||
            otpCode.Length != 6 ||
            !otpCode.All(char.IsDigit))
        {
            throw new InvalidOperationException(
                InvalidOtpMessage);
        }

        if (string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 8)
        {
            throw new InvalidOperationException(
                "New password must be at least 8 characters.");
        }

        if (confirmPassword is not null &&
            newPassword != confirmPassword)
        {
            throw new InvalidOperationException(
                "Password confirmation does not match.");
        }
    }
}