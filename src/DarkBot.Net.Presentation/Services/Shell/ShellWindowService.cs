using System.Windows;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using DarkBot.Net.Presentation.ViewModels.Shell;
using DarkBot.Net.Presentation.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DarkBot.Net.Presentation.Services.Shell;

public sealed class ShellWindowService(IServiceProvider serviceProvider) : IShellWindowService
{
    public void ShowShellWindow()
    {
        var viewModel = serviceProvider.GetRequiredService<ShellWindowViewModel>();
        var coordinator = serviceProvider.GetRequiredService<GameShutdownCoordinator>();

        Log.Information(
            "UI shell: opening window, CurrentViewModel={CurrentViewModelType}",
            viewModel.CurrentViewModel?.GetType().Name ?? "null");

        var window = new ShellWindowView(viewModel, coordinator);

        System.Windows.Application.Current.MainWindow = window;
        window.Show();
        Log.Information("UI shell: window shown");
    }
}
