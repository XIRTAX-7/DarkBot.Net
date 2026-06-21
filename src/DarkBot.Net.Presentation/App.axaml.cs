using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DarkBot.Net.Presentation.Services.Shell;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DarkBot.Net.Presentation;

public partial class App : global::Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Program.AppHost is null)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        SetupGlobalExceptionLogging();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Program.AppHost.Services
                .GetRequiredService<IShellWindowService>()
                .ShowShellWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void SetupGlobalExceptionLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled AppDomain exception (IsTerminating={IsTerminating})", args.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI thread exception");
            args.Handled = true;
        };
    }
}
