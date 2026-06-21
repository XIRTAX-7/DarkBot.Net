namespace DarkBot.Net.Application.Managers;

/// <summary>Known DarkOrbit map ids — subset ported from Java StarManager.</summary>
internal static class StarMapRegistry
{
    private static readonly Dictionary<int, (string Name, string ShortName)> Maps = new()
    {
        [1] = ("1-1", "1-1"),
        [2] = ("1-2", "1-2"),
        [3] = ("1-3", "1-3"),
        [4] = ("1-4", "1-4"),
        [5] = ("2-1", "2-1"),
        [6] = ("2-2", "2-2"),
        [7] = ("2-3", "2-3"),
        [8] = ("2-4", "2-4"),
        [9] = ("3-1", "3-1"),
        [10] = ("3-2", "3-2"),
        [11] = ("3-3", "3-3"),
        [12] = ("3-4", "3-4"),
        [13] = ("4-1", "4-1"),
        [14] = ("4-2", "4-2"),
        [15] = ("4-3", "4-3"),
        [16] = ("4-4", "4-4"),
        [17] = ("1-5", "1-5"),
        [18] = ("1-6", "1-6"),
        [19] = ("1-7", "1-7"),
        [20] = ("1-8", "1-8"),
        [21] = ("2-5", "2-5"),
        [22] = ("2-6", "2-6"),
        [23] = ("2-7", "2-7"),
        [24] = ("2-8", "2-8"),
        [25] = ("3-5", "3-5"),
        [26] = ("3-6", "3-6"),
        [27] = ("3-7", "3-7"),
        [28] = ("3-8", "3-8"),
        [29] = ("4-5", "4-5"),
        [91] = ("5-1", "5-1"),
        [92] = ("5-2", "5-2"),
        [93] = ("5-3", "5-3"),
        [94] = ("5-4", "5-4"),
        [306] = ("1BL", "1BL"),
        [307] = ("2BL", "2BL"),
        [308] = ("3BL", "3BL"),
        [401] = ("Experiment Zone 1", "EZ 1"),
        [402] = ("Experiment Zone 2-1", "EZ 2-1"),
        [403] = ("Experiment Zone 2-2", "EZ 2-2"),
        [404] = ("Experiment Zone 2-3", "EZ 2-3"),
    };

    internal static bool TryGet(int mapId, out string name, out string shortName)
    {
        if (Maps.TryGetValue(mapId, out var entry))
        {
            name = entry.Name;
            shortName = entry.ShortName;
            return true;
        }

        name = string.Empty;
        shortName = string.Empty;
        return false;
    }
}
