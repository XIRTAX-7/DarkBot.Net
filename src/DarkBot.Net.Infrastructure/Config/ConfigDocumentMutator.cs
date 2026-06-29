using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Infrastructure.Config;

/// <summary>Применяет изменения по lowercase dot path к BotProfileDocument.</summary>
internal static class ConfigDocumentMutator
{
    public static BotProfileDocument Apply<T>(BotProfileDocument document, string path, T value)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            throw new ArgumentException("Path is empty.", nameof(path));

        return segments[0].ToLowerInvariant() switch
        {
            "general" => ApplyGeneral(document, segments, value),
            "collect" => ApplyCollect(document, segments, value),
            "meta" => ApplyMeta(document, segments, value),
            _ => throw new KeyNotFoundException($"Config path not found: {path}")
        };
    }

    private static BotProfileDocument ApplyGeneral<T>(BotProfileDocument document, string[] segments, T value)
    {
        if (segments.Length != 2)
            throw new KeyNotFoundException($"Invalid general path: {string.Join('.', segments)}");

        var general = segments[1].ToLowerInvariant() switch
        {
            "current_module" when value is string module =>
                document.General with { CurrentModule = module },
            "working_map" when value is int map =>
                document.General with { WorkingMap = map },
            "safety_wait" when value is int wait =>
                document.General with { SafetyWait = wait },
            _ => throw new KeyNotFoundException($"Invalid general path: {string.Join('.', segments)}")
        };

        return document with { General = general };
    }

    private static BotProfileDocument ApplyMeta<T>(BotProfileDocument document, string[] segments, T value)
    {
        if (segments.Length != 2)
            throw new KeyNotFoundException($"Invalid meta path: {string.Join('.', segments)}");

        var meta = segments[1].ToLowerInvariant() switch
        {
            "display_name" when value is string name =>
                document.Meta with { DisplayName = name },
            _ => throw new KeyNotFoundException($"Invalid meta path: {string.Join('.', segments)}")
        };

        return document with { Meta = meta };
    }

    private static BotProfileDocument ApplyCollect<T>(BotProfileDocument document, string[] segments, T value)
    {
        if (segments.Length == 2)
        {
            var collect = segments[1].ToLowerInvariant() switch
            {
                "radius" when value is int radius =>
                    document.Collect with { Radius = radius },
                "stay_away_from_enemies" when value is bool stayAway =>
                    document.Collect with { StayAwayFromEnemies = stayAway },
                "auto_cloak" when value is bool autoCloak =>
                    document.Collect with { AutoCloak = autoCloak },
                "ignore_contested_boxes" when value is bool ignore =>
                    document.Collect with { IgnoreContestedBoxes = ignore },
                _ => throw new KeyNotFoundException($"Invalid collect path: {string.Join('.', segments)}")
            };

            return document with { Collect = collect };
        }

        if (segments.Length == 4
            && segments[1].Equals("box_infos", StringComparison.OrdinalIgnoreCase))
        {
            var boxName = segments[2];
            var boxInfos = new Dictionary<string, BoxInfoRecord>(
                document.Collect.BoxInfos,
                StringComparer.OrdinalIgnoreCase);

            if (!boxInfos.TryGetValue(boxName, out var existing))
                existing = new BoxInfoRecord(false, 0, 0);

            var updated = segments[3].ToLowerInvariant() switch
            {
                "should_collect" when value is bool shouldCollect =>
                    existing with { ShouldCollect = shouldCollect },
                "priority" when value is int priority =>
                    existing with { Priority = priority },
                "wait_time" when value is int waitTime =>
                    existing with { WaitTime = waitTime },
                _ => throw new KeyNotFoundException($"Invalid box info path: {string.Join('.', segments)}")
            };

            boxInfos[boxName] = updated;
            return document with { Collect = document.Collect with { BoxInfos = boxInfos } };
        }

        throw new KeyNotFoundException($"Invalid collect path: {string.Join('.', segments)}");
    }
}
