using System.Windows;
using System.Windows.Threading;
using DarkBot.Net.Presentation.Logging;
using DarkBot.Net.Presentation.Services.Shell;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DarkBot.Net.Presentation;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, updateAccent: false);
        ApplicationAccentColorManager.Apply(
            System.Windows.Media.Color.FromRgb(0x6D, 0x5D, 0xF6),
            ApplicationTheme.Dark);

        SetupGlobalExceptionLogging();

        if (Program.AppHost is null)
            return;

        PresentationUiDiagnostics.LogReactiveUiSetup();

        Program.AppHost.Services
            .GetRequiredService<IShellWindowService>()
            .ShowShellWindow();
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

        Current.DispatcherUnhandledException += (_, args) =>
        {
            var ex = args.Exception;
            if (ex is System.Windows.Markup.XamlParseException xamlEx)
            {
                Log.Error(
                    xamlEx,
                    "Unhandled UI thread XamlParseException (line {Line}, position {Position})",
                    xamlEx.LineNumber,
                    xamlEx.LinePosition);
            }
            else
            {
                Log.Error(ex, "Unhandled UI thread exception ({ExceptionType})", ex.GetType().Name);
            }

            args.Handled = true;
        };
    }
}
