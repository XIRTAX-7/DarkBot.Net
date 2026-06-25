using DarkBot.Net.Presentation.ViewModels;
using DarkBot.Net.Presentation.Views.Config.Pages;
using Wpf.Ui.Abstractions;

namespace DarkBot.Net.Presentation.Services.Navigation;

/// <summary>Создаёт страницы конфигурации для <see cref="Wpf.Ui.Controls.NavigationView"/>.</summary>
public sealed class ConfigNavigationPageProvider : INavigationViewPageProvider
{
    private ConfigTreeViewModel? _viewModel;

    public void Attach(ConfigTreeViewModel viewModel) => _viewModel = viewModel;

    public object? GetPage(Type pageType)
    {
        if (pageType == typeof(ConfigMainPage))
            return new ConfigMainPage();

        if (pageType == typeof(ConfigCollectPage))
            return new ConfigCollectPage(_viewModel ?? new ConfigTreeViewModel());

        if (pageType == typeof(ConfigPlaceholderPage))
            return new ConfigPlaceholderPage(_viewModel ?? new ConfigTreeViewModel());

        return null;
    }
}
