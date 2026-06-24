using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DarkBot.Net.Infrastructure.Game.Lifecycle;
using DarkBot.Net.Presentation.ViewModels.Shell;
using ReactiveUI.Avalonia;

namespace DarkBot.Net.Presentation.Views.Shell;

/// <summary>
/// Единственное окно приложения. ViewModelViewHost показывает Login или Main.
/// </summary>
public partial class ShellWindowView : ReactiveWindow<ShellWindowViewModel>
{
    private readonly GameShutdownCoordinator? _coordinator;
    private bool _shutdownStarted;

    /// <summary>Конструктор для XAML previewer.</summary>
    public ShellWindowView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public ShellWindowView(GameShutdownCoordinator coordinator)
    {
        _coordinator = coordinator;
        AvaloniaXamlLoader.Load(this);
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_shutdownStarted || _coordinator is null)
            return;

        e.Cancel = true;
        _shutdownStarted = true;

        try
        {
            await _coordinator.StopGameClientAsync().ConfigureAwait(true);
        }
        finally
        {
            Close();
        }
    }
}
