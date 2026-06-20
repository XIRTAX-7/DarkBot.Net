using Avalonia.Controls;
using Avalonia.Interactivity;
using DarkBot.Net.Ui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DarkBot.Net.Ui.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        var viewModel = Program.AppHost.Services.GetRequiredService<LoginViewModel>();
        viewModel.OwnerWindow = this;
        DataContext = viewModel;
        viewModel.LoginSucceeded += () => Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
