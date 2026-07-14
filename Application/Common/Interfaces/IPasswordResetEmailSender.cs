namespace Application.Common.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendOtpAsync(
        string recipientEmail,
        string otpCode,
        int expiryMinutes,
        CancellationToken cancellationToken = default);
}