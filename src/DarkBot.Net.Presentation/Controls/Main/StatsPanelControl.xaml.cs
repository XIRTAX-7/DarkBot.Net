using System.Windows;
using DarkBot.Net.Presentation.ViewModels.Main;
using ReactiveUI;

namespace DarkBot.Net.Presentation.Controls.Main;

public partial class StatsPanelControl : ReactiveUserControl<StatsPanelViewModel>
{
    public StatsPanelControl()
    {
        InitializeComponent();
    }
}
