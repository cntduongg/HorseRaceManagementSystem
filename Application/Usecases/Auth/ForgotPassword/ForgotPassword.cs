using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Auth.ForgotPassword;

// POST /api/auth/forgot-password — phát OTP đặt lại mật khẩu.
// Chưa có email service → trả OTP trong response (DEV). KHÔNG làm vậy ở production.
public sealed record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponse>;

public sealed record ForgotPasswordResponse(string Message, string? Otp, int ExpiresInMinutes);

public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private const int ExpiryMinutes = 15;

    private readonly IApplicationDbContext _context;

    public ForgotPasswordCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ForgotPasswordResponse> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Không tiết lộ email tồn tại hay không.
        if (user is null)
            return new ForgotPasswordResponse(
                "If the email exists, an OTP code has been sent.", null, ExpiryMinutes);

        var now = DateTime.UtcNow;

        // Vô hiệu hóa các OTP chưa dùng còn hiệu lực.
        var actives = await _context.PasswordResetOtps
            .Where(o => o.UserId == user.UserId && o.UsedAt == null && o.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var o in actives)
            o.UsedAt = now;

        var code = Random.Shared.Next(0, 1_000_000).ToString("D6");

        _context.PasswordResetOtps.Add(new PasswordResetOtp
        {
            UserId = user.UserId,
            OtpCode = code,
            ExpiresAt = now.AddMinutes(ExpiryMinutes),
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponse(
            "An OTP code has been generated.", code, ExpiryMinutes);
    }
}
