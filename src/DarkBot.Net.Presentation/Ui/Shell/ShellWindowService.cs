using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Presentation.Ui.Shell;
using DarkBot.Net.Presentation.ViewModels.Shell;
using DarkBot.Net.Presentation.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DarkBot.Net.Presentation.Ui.Shell;

public sealed class ShellWindowService(
    IServiceProvider serviceProvider,
    IGameShutdownAppService gameShutdown) : IShellWindowService
{
    public void ShowShellWindow()
    {
        var viewModel = serviceProvider.GetRequiredService<ShellWindowViewModel>();

        Log.Information(
            "UI shell: opening window, CurrentViewModel={CurrentViewModelType}",
            viewModel.CurrentViewModel?.GetType().Name ?? "null");

        var window = new ShellWindowView(viewModel, gameShutdown);

        System.Windows.Application.Current.MainWindow = window;
        window.Show();
        Log.Information("UI shell: window shown");
    }
}
