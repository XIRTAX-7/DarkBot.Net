using Avalonia.Markup.Xaml;
using DarkBot.Net.Presentation.ViewModels.Shell;
using ReactiveUI.Avalonia;

namespace DarkBot.Net.Presentation.Views.Shell;

/// <summary>
/// Единственное окно приложения. ViewModelViewHost показывает Login или Main.
/// </summary>
public partial class ShellWindowView : ReactiveWindow<ShellWindowViewModel>
{
    /// <summary>Конструктор для XAML previewer.</summary>
    public ShellWindowView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
