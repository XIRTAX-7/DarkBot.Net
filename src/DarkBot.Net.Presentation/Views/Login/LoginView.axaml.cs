using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DarkBot.Net.Presentation.ViewModels;
using ReactiveUI.Avalonia;

namespace DarkBot.Net.Presentation.Views.Login;

public partial class LoginView : ReactiveUserControl<LoginViewModel>
{
    public LoginView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
