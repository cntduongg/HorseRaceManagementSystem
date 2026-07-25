namespace Domain.Common.PhoneNumbers;

public sealed class InvalidPhoneNumberException : Exception
{
    public InvalidPhoneNumberException()
        : base("Số điện thoại không hợp lệ.")
    {
    }
}