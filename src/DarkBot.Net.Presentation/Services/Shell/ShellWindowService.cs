using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DarkBot.Net.Infrastructure.Game;
using DarkBot.Net.Presentation.ViewModels.Shell;
using DarkBot.Net.Presentation.Views.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Presentation.Services.Shell;

public sealed class ShellWindowService(IServiceProvider serviceProvider) : IShellWindowService
{
    public void ShowShellWindow()
    {
        var viewModel = serviceProvider.GetRequiredService<ShellWindowViewModel>();
        var coordinator = serviceProvider.GetRequiredService<GameShutdownCoordinator>();
        var window = new ShellWindowView(coordinator)
        {
            ViewModel = viewModel
        };

        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = window;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        window.Show();
    }
}
