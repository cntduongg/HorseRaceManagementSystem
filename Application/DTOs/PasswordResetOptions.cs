namespace Application.DTOs;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int ExpiryMinutes { get; set; } = 15;

    public int MaxFailedAttempts { get; set; } = 5;

    public bool ReturnOtpInResponse { get; set; }

    public string OtpPepper { get; set; } = string.Empty;

    public string FrontendBaseUrl { get; set; } = string.Empty;
}