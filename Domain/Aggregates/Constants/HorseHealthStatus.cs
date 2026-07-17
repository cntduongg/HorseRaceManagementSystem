namespace Domain.Aggregates.Constants;

public static class HorseHealthStatus
{
    public const string Healthy = "Healthy";     // 3 thanh thể lực - Khỏe mạnh
    public const string Fair = "Fair";           // 2 thanh thể lực - Tạm ổn
    public const string Weak = "Weak";           // 1 thanh thể lực - Yếu
    public const string Exhausted = "Exhausted"; // 0 thanh thể lực - Kiệt sức
}