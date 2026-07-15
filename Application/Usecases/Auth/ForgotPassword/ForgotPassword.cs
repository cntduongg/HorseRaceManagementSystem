using System.Security.Cryptography;
using Application.Common;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Usecases.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email)
    : IRequest<ForgotPasswordResponse>;

public sealed record ForgotPasswordResponse(
    string Message,
    string? Otp,
    int ExpiresInMinutes);

public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<
        ForgotPasswordCommand,
        ForgotPasswordResponse>
{
    private const string GenericMessage =
        "If the email exists, an OTP code has been sent.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordResetOtpProtector _otpProtector;
    private readonly IPasswordResetEmailSender _emailSender;
    private readonly PasswordResetOptions _options;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordResetOtpProtector otpProtector,
        IPasswordResetEmailSender emailSender,
        IOptions<PasswordResetOptions> options)
    {
        _context = context;
        _otpProtector = otpProtector;
        _emailSender = emailSender;
        _options = options.Value;
    }

    public async Task<ForgotPasswordResponse> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InvalidOperationException(
                "Email is required.");
        }

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Email == email,
                cancellationToken);

        // Không tiết lộ email có tồn tại hay không.
        if (user is null)
        {
            return new ForgotPasswordResponse(
                GenericMessage,
                Otp: null,
                _options.ExpiryMinutes);
        }

        var now = DateTime.UtcNow;

        // Vô hiệu hóa toàn bộ OTP cũ chưa dùng.
        var unusedOtps = await _context.PasswordResetOtps
            .Where(o =>
                o.UserId == user.UserId &&
                o.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var unusedOtp in unusedOtps)
        {
            unusedOtp.UsedAt = now;
        }

        var otpCode = RandomNumberGenerator
            .GetInt32(0, 1_000_000)
            .ToString("D6");

        _context.PasswordResetOtps.Add(
            new PasswordResetOtp
            {
                UserId = user.UserId,
                OtpCodeHash =
                    _otpProtector.Hash(otpCode),
                FailedAttempts = 0,
                ExpiresAt = now.AddMinutes(
                    _options.ExpiryMinutes),
                CreatedAt = now
            });

        // Lưu OTP trước, tránh trường hợp gửi email thành công
        // nhưng database lại không lưu được OTP.
        await _context.SaveChangesAsync(
            cancellationToken);

        await _emailSender.SendOtpAsync(
            recipientEmail: email,
            otpCode: otpCode,
            expiryMinutes: _options.ExpiryMinutes,
            cancellationToken);

        return new ForgotPasswordResponse(
            GenericMessage,
            _options.ReturnOtpInResponse
                ? otpCode
                : null,
            _options.ExpiryMinutes);
    }
}