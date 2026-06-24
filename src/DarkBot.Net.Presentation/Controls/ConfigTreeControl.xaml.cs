using System.Windows;
using DarkBot.Net.Presentation.ViewModels;
using ReactiveUI;

namespace DarkBot.Net.Presentation.Controls;

public partial class ConfigTreeControl : ReactiveUserControl<ConfigTreeViewModel>
{
    public ConfigTreeControl()
    {
        InitializeComponent();
    }
}
