namespace DarkBot.Net.Infrastructure.Game;

/// <summary>
/// Флаги жизненного цикла клиента: отличает намеренный shutdown от случайного закрытия окна.
/// </summary>
public sealed class GameClientLifecycle
{
    private volatile bool _intentionalShutdown;

    public bool IntentionalShutdown => _intentionalShutdown;

    public void MarkIntentionalShutdown() => _intentionalShutdown = true;
}
