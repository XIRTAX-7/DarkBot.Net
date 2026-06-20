namespace DarkBot.Net.Api;

public interface IApi
{
    /// <summary>Only one instance may exist in the plugin/DI context.</summary>
    public interface ISingleton : IApi;
}
