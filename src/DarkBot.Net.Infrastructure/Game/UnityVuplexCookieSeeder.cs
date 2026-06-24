using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Записывает dosid в Chromium cookie store Vuplex до старта DarkOrbit.exe
/// (аналог session.cookies.set в legacy Electron-клиенте).
/// </summary>
public sealed class UnityVuplexCookieSeeder(ILogger<UnityVuplexCookieSeeder> logger)
{
    private const string CookieDatabaseRelativePath =
        "Bigpoint\\DarkOrbit\\Vuplex.WebView\\chromium-cache\\Network\\Cookies";

    public void SeedDosid(string instanceHost, string sid) =>
        SeedDosidAtPath(ResolveCookieDatabasePath(), instanceHost, sid, logger);

    internal static void SeedDosidAtPath(
        string cookiePath,
        string instanceHost,
        string sid,
        ILogger? logger = null)
    {
        logger ??= NullLogger<UnityVuplexCookieSeeder>.Instance;

        if (string.IsNullOrWhiteSpace(instanceHost) || string.IsNullOrWhiteSpace(sid))
            return;

        if (!File.Exists(cookiePath))
        {
            logger.LogWarning(
                "Vuplex cookie database not found at {Path} — run the game once manually or login will be required",
                cookiePath);
            return;
        }

        var trimmedSid = sid.Trim();
        var hosts = BuildHostKeys(instanceHost);

        try
        {
            using var connection = new SqliteConnection($"Data Source={cookiePath};Mode=ReadWrite");
            connection.Open();

            var columns = ReadCookieColumns(connection);
            if (columns.Count == 0)
            {
                logger.LogWarning("Vuplex cookie table schema is empty at {Path}", cookiePath);
                return;
            }

            foreach (var hostKey in hosts)
            {
                DeleteDosid(connection, hostKey);
                InsertDosid(connection, columns, hostKey, trimmedSid);
                logger.LogInformation(
                    "Seeded dosid cookie for Unity WebView host={Host}, sidSuffix={SidSuffix}",
                    hostKey,
                    trimmedSid.Length >= 4 ? trimmedSid[^4..] : trimmedSid);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to seed dosid cookie at {Path} — Unity may show login screen",
                cookiePath);
        }
    }

    internal static string ResolveCookieDatabasePath()
    {
        var localLow = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData",
            "LocalLow");

        return Path.Combine(localLow, CookieDatabaseRelativePath);
    }

    private static IReadOnlyList<string> BuildHostKeys(string instanceHost)
    {
        var host = instanceHost.Trim();
        if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            host = new Uri(host).Host;
        }

        var dottedInstance = host.StartsWith('.') ? host : $".{host}";
        return [dottedInstance, ".darkorbit.com"];
    }

    private static IReadOnlyList<string> ReadCookieColumns(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(cookies)";

        var columns = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(1));

        return columns;
    }

    private static void DeleteDosid(SqliteConnection connection, string hostKey)
    {
        using var delete = connection.CreateCommand();
        delete.CommandText = """
            DELETE FROM cookies
            WHERE host_key = $host AND name = 'dosid'
            """;
        delete.Parameters.AddWithValue("$host", hostKey);
        delete.ExecuteNonQuery();
    }

    private static void InsertDosid(
        SqliteConnection connection,
        IReadOnlyList<string> columns,
        string hostKey,
        string sid)
    {
        var now = ToChromeTimestamp(DateTime.UtcNow);
        var values = new Dictionary<string, object?>
        {
            ["creation_utc"] = now,
            ["host_key"] = hostKey,
            ["top_frame_site_key"] = hostKey,
            ["name"] = "dosid",
            ["value"] = sid,
            ["encrypted_value"] = Array.Empty<byte>(),
            ["path"] = "/",
            ["expires_utc"] = 0L,
            ["is_secure"] = 1L,
            ["is_httponly"] = 0L,
            ["last_access_utc"] = now,
            ["has_expires"] = 0L,
            ["is_persistent"] = 1L,
            ["priority"] = 1L,
            ["samesite"] = -1L,
            ["source_scheme"] = 2L,
            ["source_port"] = 443L,
            ["is_same_party"] = 0L,
        };

        var insertColumns = columns.Where(values.ContainsKey).ToList();
        if (insertColumns.Count == 0)
            throw new InvalidOperationException("No compatible columns found in Vuplex cookies table.");

        using var insert = connection.CreateCommand();
        insert.CommandText = $"""
            INSERT INTO cookies ({string.Join(", ", insertColumns)})
            VALUES ({string.Join(", ", insertColumns.Select(c => "$" + c))})
            """;

        foreach (var column in insertColumns)
        {
            var value = values[column];
            if (value is byte[] bytes)
                insert.Parameters.AddWithValue("$" + column, bytes);
            else
                insert.Parameters.AddWithValue("$" + column, value);
        }

        insert.ExecuteNonQuery();
    }

    private static long ToChromeTimestamp(DateTime utc) =>
        (utc - new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10;
}
