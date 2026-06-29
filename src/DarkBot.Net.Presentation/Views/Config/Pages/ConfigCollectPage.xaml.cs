using System.Windows.Controls;
using DarkBot.Net.Presentation.ViewModels.Config;

namespace DarkBot.Net.Presentation.Views.Config.Pages;

public partial class ConfigCollectPage : Page
{
    public ConfigCollectPage(ConfigTreeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        CollectSection.ViewModel = viewModel;
        CollectSection.DataContext = viewModel;
    }

    public ConfigTreeViewModel ViewModel { get; }
}
