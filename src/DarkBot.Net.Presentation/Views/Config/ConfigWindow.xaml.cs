using DarkBot.Net.Presentation.ViewModels.Config;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Views.Config;

public partial class ConfigWindow : FluentWindow
{
    public ConfigWindow()
        : this(null)
    {
    }

    public ConfigWindow(ConfigTreeViewModel? viewModel)
    {
        Log.Debug("UI config: ConfigWindow ctor start");
        InitializeComponent();
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, updateAccent: false);
        ConfigTree.AttachViewModel(viewModel ?? new ConfigTreeViewModel());
        ProfilePanel.DataContext = ConfigTree.ViewModel;
        Closed += (_, _) => Log.Debug("UI config: ConfigWindow closed");
        Log.Debug("UI config: ConfigWindow ctor complete");
    }
}
