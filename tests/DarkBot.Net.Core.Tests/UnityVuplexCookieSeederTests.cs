using DarkBot.Net.Infrastructure.Game;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace DarkBot.Net.Application.Tests;

public sealed class UnityVuplexCookieSeederTests
{
    [Fact]
    public void SeedDosidAtPath_WritesCookieIntoVuplexChromiumSchema()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var cookiePath = Path.Combine(tempDir, "Cookies");

        CreateVuplexCookieDatabase(cookiePath);

        UnityVuplexCookieSeeder.SeedDosidAtPath(
            cookiePath,
            "ru1.darkorbit.com",
            "abc123session",
            NullLogger<UnityVuplexCookieSeeder>.Instance);

        using var connection = new SqliteConnection($"Data Source={cookiePath};Mode=ReadOnly");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM cookies
            WHERE name = 'dosid' AND value = 'abc123session'
            """;
        var count = Convert.ToInt32(command.ExecuteScalar());

        Assert.Equal(2, count);
    }

    private static void CreateVuplexCookieDatabase(string cookiePath)
    {
        using var connection = new SqliteConnection($"Data Source={cookiePath};Mode=ReadWriteCreate");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE cookies (
                creation_utc INTEGER NOT NULL,
                host_key TEXT NOT NULL,
                top_frame_site_key TEXT NOT NULL,
                name TEXT NOT NULL,
                value TEXT NOT NULL,
                encrypted_value BLOB NOT NULL,
                path TEXT NOT NULL,
                expires_utc INTEGER NOT NULL,
                is_secure INTEGER NOT NULL,
                is_httponly INTEGER NOT NULL,
                last_access_utc INTEGER NOT NULL,
                has_expires INTEGER NOT NULL,
                is_persistent INTEGER NOT NULL,
                priority INTEGER NOT NULL,
                samesite INTEGER NOT NULL,
                source_scheme INTEGER NOT NULL,
                source_port INTEGER NOT NULL,
                is_same_party INTEGER NOT NULL)
            """;
        command.ExecuteNonQuery();
    }
}
