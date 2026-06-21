using Avalonia.Controls;
using Avalonia.Interactivity;
using DarkBot.Net.Infrastructure.Game;

namespace DarkBot.Net.Presentation.Views;

public partial class ProcessPickerWindow : Window
{
    public ProcessPickerWindow()
    {
        InitializeComponent();
    }

    public ProcessPickerWindow(IReadOnlyList<GameProcessInfo> processes) : this()
    {
        ProcessList.ItemsSource = processes;
    }

    public int? SelectedPid { get; private set; }

    private void OnAttachClick(object? sender, RoutedEventArgs e)
    {
        if (ProcessList.SelectedItem is GameProcessInfo process)
        {
            SelectedPid = process.Pid;
            Close(true);
            return;
        }

        Close(false);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
