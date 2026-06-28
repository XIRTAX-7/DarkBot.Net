using System.Windows.Threading;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Presentation.Ui;
using DarkBot.Net.Presentation.ViewModels.Shell;

namespace DarkBot.Net.Presentation.Ui.Shell;

/// <summary>
/// Периодически обновляет title bar из read-only метрик бота.
/// Использует <see cref="IBotDiagnosticsAppService"/> — без Frida RPC и без tick менеджеров.
/// </summary>
public sealed class TitleBarDiagnosticsUiCoordinator(
    IBotDiagnosticsAppService botDiagnostics,
    TitleBarDiagnosticsViewModel viewModel)
{
    private static readonly TimeSpan RefreshInterval = UiRefreshIntervals.Dashboard;

    private DispatcherTimer? _timer;

    public void Attach(Dispatcher dispatcher)
    {
        if (_timer is not null)
            return;

        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = RefreshInterval
        };
        _timer.Tick += OnTick;
        _timer.Start();
        Refresh();
    }

    public void Detach()
    {
        if (_timer is null)
            return;

        _timer.Tick -= OnTick;
        _timer.Stop();
        _timer = null;
    }

    public void Refresh() => viewModel.Apply(botDiagnostics.Get());

    private void OnTick(object? sender, EventArgs e) => Refresh();
}
