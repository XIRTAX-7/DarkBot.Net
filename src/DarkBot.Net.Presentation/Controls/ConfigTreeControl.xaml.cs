using DarkBot.Net.Presentation.Services.Navigation;
using DarkBot.Net.Presentation.ViewModels;
using DarkBot.Net.Presentation.Views.Config.Pages;
using ReactiveUI;
using Serilog;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Controls;

public partial class ConfigTreeControl : ReactiveUserControl<ConfigTreeViewModel>
{
    private readonly ConfigNavigationPageProvider _pageProvider = new();
    private bool _initialNavigationDone;

    public ConfigTreeControl()
    {
        InitializeComponent();

        ConfigNavigation.SetPageProviderService(_pageProvider);
        ConfigNavigation.SelectionChanged += OnNavigationSelectionChanged;
        Loaded += OnLoaded;
    }

    public void AttachViewModel(ConfigTreeViewModel viewModel)
    {
        ViewModel = viewModel;
        _pageProvider.Attach(viewModel);
        TryNavigateToMainPage();
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e) =>
        TryNavigateToMainPage();

    private void TryNavigateToMainPage()
    {
        if (_initialNavigationDone || ViewModel is null || !IsLoaded)
            return;

        try
        {
            if (!ConfigNavigation.Navigate(typeof(ConfigMainPage)))
                Log.Warning("UI config: NavigationView.Navigate(ConfigMainPage) returned false");
            else
                _initialNavigationDone = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UI config: NavigationView initial navigation failed");
            throw;
        }
    }

    private void OnNavigationSelectionChanged(NavigationView sender, System.Windows.RoutedEventArgs e)
    {
        if (ViewModel is null || sender.SelectedItem is not NavigationViewItem item)
            return;

        var section = item.Tag as ConfigSidebarSection?
            ?? item.TargetPageType switch
            {
                Type t when t == typeof(ConfigMainPage) => ConfigSidebarSection.Main,
                Type t when t == typeof(ConfigCollectPage) => ConfigSidebarSection.Collect,
                _ => ViewModel.SelectedSidebarItem?.Section ?? ConfigSidebarSection.Main,
            };

        var sidebarItem = ConfigTreeViewModel.SidebarItems
            .FirstOrDefault(i => i.Section == section);

        if (sidebarItem is not null)
            ViewModel.SelectedSidebarItem = sidebarItem;
    }
}
