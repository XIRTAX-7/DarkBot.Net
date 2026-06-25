using System.Windows.Controls;
using DarkBot.Net.Presentation.ViewModels;

namespace DarkBot.Net.Presentation.Views.Config.Pages;

public partial class ConfigCollectPage : Page
{
    public ConfigCollectPage(ConfigTreeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        CollectSection.ViewModel = viewModel;
    }

    public ConfigTreeViewModel ViewModel { get; }
}
