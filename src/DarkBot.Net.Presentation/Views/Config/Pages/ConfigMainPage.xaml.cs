using System.Windows.Controls;
using DarkBot.Net.Presentation.ViewModels;

namespace DarkBot.Net.Presentation.Views.Config.Pages;

public partial class ConfigMainPage : Page
{
    public ConfigMainPage()
        : this(new ConfigTreeViewModel())
    {
    }

    public ConfigMainPage(ConfigTreeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    public ConfigTreeViewModel ViewModel { get; }
}
