using System.Text.Json.Serialization;

namespace DarkBot.Net.Core.Config;

/// <summary>Владелец профиля: пользователь или AI.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProfileOwner>))]
public enum ProfileOwner
{
    User,
    Ai
}
