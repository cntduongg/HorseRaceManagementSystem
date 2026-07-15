namespace Application.Common;

public interface IPasswordResetOtpProtector
{
    string Hash(string otpCode);

    bool Verify(string otpCode, string otpCodeHash);
}