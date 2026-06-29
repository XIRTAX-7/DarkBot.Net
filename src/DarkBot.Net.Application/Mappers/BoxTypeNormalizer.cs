namespace DarkBot.Net.Application.Mappers;

/// <summary>Нормализация type/hash бокса — порт Box.java (trait asset id).</summary>
public static class BoxTypeNormalizer
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var type = raw.Trim();
        if (type.Length > 5)
        {
            var comma = type.IndexOf(',');
            type = comma > 0 ? type[..comma] : type;
        }

        if (type.Equals("bonus_box", StringComparison.OrdinalIgnoreCase)
            || type.Equals("BONUS_BOX", StringComparison.OrdinalIgnoreCase))
            return "BONUS_BOX";

        if (type.Length > 5)
        {
            type = type.Replace("box_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("_box", "", StringComparison.OrdinalIgnoreCase);
        }

        if (type.StartsWith("ore_", StringComparison.OrdinalIgnoreCase))
            type = type[4..];

        if (type.Equals("bonus", StringComparison.OrdinalIgnoreCase))
            return "BONUS_BOX";

        return type.ToUpperInvariant();
    }
}
