using Application.Common.Interfaces;
using Infrastructure.Options;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services;

public sealed class SmtpPasswordResetEmailSender
    : IPasswordResetEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpPasswordResetEmailSender(
        IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendOtpAsync(
        string recipientEmail,
        string otpCode,
        int expiryMinutes,
        CancellationToken cancellationToken = default)
    {
        // Development có thể tắt SMTP và lấy OTP trong response.
        if (!_options.Enabled)
        {
            return;
        }

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _options.FromName,
            _options.FromEmail));

        message.To.Add(MailboxAddress.Parse(recipientEmail));

        message.Subject = "Password reset verification code";

        message.Body = new TextPart("plain")
        {
            Text =
                $"""
                You requested to reset your password.

                Your verification code is:

                {otpCode}

                This code expires in {expiryMinutes} minutes.

                If you did not request a password reset,
                you can ignore this email.
                """
        };

        using var client = new SmtpClient();

        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            socketOptions,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            await client.AuthenticateAsync(
                _options.UserName,
                _options.Password,
                cancellationToken);
        }

        await client.SendAsync(
            message,
            cancellationToken);

        await client.DisconnectAsync(
            quit: true,
            cancellationToken);
    }
}