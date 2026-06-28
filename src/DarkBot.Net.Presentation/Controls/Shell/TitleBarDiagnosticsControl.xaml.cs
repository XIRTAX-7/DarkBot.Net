using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DarkBot.Net.Presentation.ViewModels.Shell;
using ReactiveUI;

namespace DarkBot.Net.Presentation.Controls.Shell;

public partial class TitleBarDiagnosticsControl : ReactiveUserControl<TitleBarDiagnosticsViewModel>
{
    public TitleBarDiagnosticsControl()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm is not null)
                .Subscribe(vm => DataContext = vm)
                .DisposeWith(disposables);
        });
    }
}
