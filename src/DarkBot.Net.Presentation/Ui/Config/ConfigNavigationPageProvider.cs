using DarkBot.Net.Presentation.ViewModels.Config;
using DarkBot.Net.Presentation.Views.Config.Pages;
using Wpf.Ui.Abstractions;

namespace DarkBot.Net.Presentation.Ui.Config;

/// <summary>Создаёт страницы конфигурации для <see cref="Wpf.Ui.Controls.NavigationView"/>.</summary>
public sealed class ConfigNavigationPageProvider : INavigationViewPageProvider
{
    private ConfigTreeViewModel? _viewModel;

    public void Attach(ConfigTreeViewModel viewModel) => _viewModel = viewModel;

    public object? GetPage(Type pageType)
    {
        if (_viewModel is null)
            return null;

        if (pageType == typeof(ConfigMainPage))
            return new ConfigMainPage(_viewModel);

        if (pageType == typeof(ConfigCollectPage))
            return new ConfigCollectPage(_viewModel);

        if (pageType == typeof(ConfigPlaceholderPage))
            return new ConfigPlaceholderPage(_viewModel);

        return null;
    }
}
