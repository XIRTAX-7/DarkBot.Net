using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Windows;
using System.Windows.Controls;
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

        this.WhenActivated(disposables =>
        {
            if (ViewModel is null)
                return;

            PasswordBox.PasswordChanged += OnPasswordChanged;
            disposables.Add(Disposable.Create(() => PasswordBox.PasswordChanged -= OnPasswordChanged));

            if (!string.IsNullOrEmpty(ViewModel.Password))
                PasswordBox.Password = ViewModel.Password;
        });
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null && sender is PasswordBox passwordBox)
            ViewModel.Password = passwordBox.Password;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) =>
        System.Windows.Application.Current.Shutdown();
}
