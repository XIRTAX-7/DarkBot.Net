using Avalonia.Controls;
using DarkBot.Net.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Presentation.Views;

public partial class ConfigWindow : Window
{
    public ConfigWindow()
    {
        InitializeComponent();
        ConfigTree.DataContext = Program.AppHost.Services.GetRequiredService<ConfigTreeViewModel>();
    }
}
