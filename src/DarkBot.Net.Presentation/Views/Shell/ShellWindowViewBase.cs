using DarkBot.Net.Presentation.ViewModels.Shell;
using DarkBot.Net.Presentation.Views;

namespace DarkBot.Net.Presentation.Views.Shell;

/// <summary>Базовое окно shell для XAML (закрытый generic <see cref="ReactiveFluentWindow{TViewModel}"/>).</summary>
public class ShellWindowViewBase : ReactiveFluentWindow<ShellWindowViewModel>;
