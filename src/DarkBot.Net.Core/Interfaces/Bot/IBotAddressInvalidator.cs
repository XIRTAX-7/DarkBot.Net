namespace DarkBot.Net.Core.Interfaces.Bot;

/// <summary>Сброс runtime-адресов бота при перезапуске игрового клиента.</summary>
public interface IBotAddressInvalidator
{
    void MarkInvalid();
}
