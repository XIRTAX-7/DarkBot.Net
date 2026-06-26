using System.Windows;
using ReactiveUI;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation.Views.Shared;

/// <summary>
/// Окно WPF-UI с поддержкой ReactiveUI (<see cref="IViewFor{T}"/>, <see cref="IActivatableView"/>).
/// </summary>
public class ReactiveFluentWindow<TViewModel> : FluentWindow, IViewFor<TViewModel>, IActivatableView
    where TViewModel : class
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(TViewModel),
            typeof(ReactiveFluentWindow<TViewModel>),
            new PropertyMetadata(null));

    public ViewModelActivator Activator { get; } = new();

    public TViewModel? ViewModel
    {
        get => (TViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel?)value;
    }
}
