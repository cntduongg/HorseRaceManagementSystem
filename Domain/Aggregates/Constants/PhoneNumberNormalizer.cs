namespace Domain.Common.PhoneNumbers;

public static class PhoneNumberNormalizer
{
    public static string? Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var digits = new string(
            phoneNumber
                .Where(char.IsDigit)
                .ToArray());

        // 0084912345678 -> 84912345678
        if (digits.StartsWith("0084", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        // 0912345678 -> 84912345678
        if (digits.StartsWith('0') && digits.Length == 10)
        {
            digits = $"84{digits[1..]}";
        }
        // 912345678 -> 84912345678
        else if (digits.Length == 9)
        {
            digits = $"84{digits}";
        }
        // 84912345678 -> giữ nguyên
        else if (!(digits.StartsWith("84", StringComparison.Ordinal)
                   && digits.Length == 11))
        {
            throw new InvalidPhoneNumberException();
        }

        ValidateVietnameseMobileNumber(digits);

        return digits;
    }

    private static void ValidateVietnameseMobileNumber(
        string normalizedPhoneNumber)
    {
        if (normalizedPhoneNumber.Length != 11 ||
            !normalizedPhoneNumber.StartsWith(
                "84",
                StringComparison.Ordinal))
        {
            throw new InvalidPhoneNumberException();
        }

        // 849..., 848..., 847..., 845..., 843...
        var validMobilePrefixes = new[]
        {
            "843",
            "845",
            "847",
            "848",
            "849"
        };

        var validPrefix = validMobilePrefixes.Any(
            prefix => normalizedPhoneNumber.StartsWith(
                prefix,
                StringComparison.Ordinal));

        if (!validPrefix)
        {
            throw new InvalidPhoneNumberException();
        }
    }
}