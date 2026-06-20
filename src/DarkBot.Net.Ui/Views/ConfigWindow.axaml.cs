using Avalonia.Controls;
using DarkBot.Net.Ui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Ui.Views;

public partial class ConfigWindow : Window
{
    public ConfigWindow()
    {
        InitializeComponent();
        ConfigTree.DataContext = Program.AppHost.Services.GetRequiredService<ConfigTreeViewModel>();
        PluginPanel.DataContext = Program.AppHost.Services.GetRequiredService<PluginPanelViewModel>();
    }
}
