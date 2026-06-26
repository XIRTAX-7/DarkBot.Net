using DarkBot.Net.Presentation.ViewModels.Config;
using DarkBot.Net.Presentation.Views.Config;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DarkBot.Net.Presentation.Services;

public sealed class ConfigWindowService(IServiceProvider serviceProvider) : IConfigWindowService
{
    private ConfigWindow? _window;

    public void Show()
    {
        try
        {
            if (_window is null || !_window.IsLoaded)
            {
                Log.Information("UI config: creating ConfigWindow");
                _window = new ConfigWindow(serviceProvider.GetRequiredService<ConfigTreeViewModel>());
            }

            var owner = System.Windows.Application.Current.MainWindow;
            if (owner is not null && !ReferenceEquals(owner, _window))
                _window.Owner = owner;

            _window.Show();
            _window.Activate();
            Log.Information("UI config: ConfigWindow shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UI config: failed to open ConfigWindow");
            _window = null;
            throw;
        }
    }
}
