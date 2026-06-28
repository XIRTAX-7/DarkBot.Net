using System.Windows.Threading;
using DarkBot.Net.Presentation.ViewModels.Main;

namespace DarkBot.Net.Presentation.Ui.Main;

/// <summary>
/// Периодически обновляет main dashboard через <see cref="MainWindowViewModel.Refresh"/>.
/// Таймер живёт здесь, а не в code-behind view.
/// </summary>
public sealed class MainDashboardUiCoordinator(MainWindowViewModel viewModel)
{
    private DispatcherTimer? _timer;

    public void Attach(Dispatcher dispatcher)
    {
        if (_timer is not null)
            return;

        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = UiRefreshIntervals.Dashboard
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

    public void Refresh() => viewModel.Refresh();

    private void OnTick(object? sender, EventArgs e) => Refresh();
}
