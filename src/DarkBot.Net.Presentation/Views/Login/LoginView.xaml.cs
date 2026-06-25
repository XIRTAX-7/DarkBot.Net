using System.Windows;
using DarkBot.Net.Presentation.ViewModels;
using ReactiveUI;
using Serilog;

namespace DarkBot.Net.Presentation.Views.Login;

public partial class LoginView : ReactiveUserControl<LoginViewModel>
{
    public LoginView()
    {
        Log.Information("UI view: LoginView created");
        InitializeComponent();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) =>
        System.Windows.Application.Current.Shutdown();
}
