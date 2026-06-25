using System.Windows.Controls;
using DarkBot.Net.Presentation.ViewModels;

namespace DarkBot.Net.Presentation.Views.Config.Pages;

public partial class ConfigPlaceholderPage : Page
{
    public ConfigPlaceholderPage(ConfigTreeViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public ConfigTreeViewModel ViewModel { get; }
}
