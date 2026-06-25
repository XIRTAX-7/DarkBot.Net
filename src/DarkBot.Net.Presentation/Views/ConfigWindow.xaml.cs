using DarkBot.Net.Presentation.Controls;
using DarkBot.Net.Presentation.ViewModels;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Views;

public partial class ConfigWindow : FluentWindow
{
    public ConfigWindow()
        : this(null)
    {
    }

    public ConfigWindow(ConfigTreeViewModel? viewModel)
    {
        Log.Debug("UI config: ConfigWindow ctor start");
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        ConfigTree.AttachViewModel(viewModel ?? new ConfigTreeViewModel());
        Closed += (_, _) => Log.Debug("UI config: ConfigWindow closed");
        Log.Debug("UI config: ConfigWindow ctor complete");
    }
}
