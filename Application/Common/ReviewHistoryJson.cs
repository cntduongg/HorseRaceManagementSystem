using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Common;

/// <summary>
/// Shared helpers for ReviewHistory BeforeData/AfterData JSON snapshots.
/// </summary>
public static class ReviewHistoryJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string? Serialize(object? value)
    {
        if (value is null)
            return null;

        return JsonSerializer.Serialize(value, Options);
    }

    public static JsonElement? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
