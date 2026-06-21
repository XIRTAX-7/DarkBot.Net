using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DarkBot.Net.Presentation;

public partial class App : global::Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        SetupGlobalExceptionLogging();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var backpage = Program.AppHost.Services.GetRequiredService<IBackpageApi>();
            if (!backpage.IsInstanceValid())
            {
                Log.Information("No valid session — opening login dialog");
                mainWindow.Opened += OnMainWindowOpenedShowLogin;
                void OnMainWindowOpenedShowLogin(object? sender, EventArgs e)
                {
                    mainWindow.Opened -= OnMainWindowOpenedShowLogin;
                    new LoginWindow().ShowDialog(mainWindow);
                }
            }
            else
            {
                Log.Information(
                    "Existing session detected: userId={UserId}, instance={Instance}",
                    backpage.UserId,
                    backpage.InstanceUri?.Host);
            }
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
