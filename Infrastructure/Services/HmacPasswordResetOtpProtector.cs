using System.Security.Cryptography;
using System.Text;
using Application.Common;
using Application.DTOs;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed class HmacPasswordResetOtpProtector
    : IPasswordResetOtpProtector
{
    private readonly byte[] _key;

    public HmacPasswordResetOtpProtector(
        IOptions<PasswordResetOptions> options)
    {
        var pepper = options.Value.OtpPepper;

        if (string.IsNullOrWhiteSpace(pepper))
        {
            throw new InvalidOperationException(
                "PasswordReset:OtpPepper is not configured.");
        }

        _key = Encoding.UTF8.GetBytes(pepper);
    }

    public string Hash(string otpCode)
    {
        using var hmac = new HMACSHA256(_key);

        var otpBytes = Encoding.UTF8.GetBytes(otpCode);
        var hashBytes = hmac.ComputeHash(otpBytes);

        return Convert.ToHexString(hashBytes);
    }

    public bool Verify(
        string otpCode,
        string otpCodeHash)
    {
        if (string.IsNullOrWhiteSpace(otpCodeHash))
        {
            return false;
        }

        try
        {
            using var hmac = new HMACSHA256(_key);

            var otpBytes = Encoding.UTF8.GetBytes(otpCode);
            var actualHash = hmac.ComputeHash(otpBytes);
            var expectedHash = Convert.FromHexString(otpCodeHash);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}