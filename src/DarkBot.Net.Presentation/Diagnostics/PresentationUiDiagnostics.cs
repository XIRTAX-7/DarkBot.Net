using DarkBot.Net.Presentation.ViewModels.Config;
using DarkBot.Net.Presentation.ViewModels.Login;
using DarkBot.Net.Presentation.ViewModels.Main;
using DarkBot.Net.Presentation.ViewModels.Shell;
using ReactiveUI;
using Serilog;
using Splat;
using System.Windows;
using System.Windows.Controls;

namespace DarkBot.Net.Presentation.Diagnostics;

/// <summary>
/// Диагностика резолва View и состояния PageHost (пустой Shell).
/// </summary>
internal static class PresentationUiDiagnostics
{
    public static void LogReactiveUiSetup()
    {
        Log.Information(
            "UI diagnostics: ReactiveUI Locator={LocatorType}",
            Locator.Current?.GetType().FullName ?? "null");

        ProbeSplatRegistration<LoginViewModel>();
        ProbeSplatRegistration<MainWindowViewModel>();
        ProbeSplatRegistration<StatsPanelViewModel>();
        ProbeSplatRegistration<ConfigTreeViewModel>();
    }

    public static void LogPageHostState(ContentControl? pageHost)
    {
        if (pageHost is null)
        {
            Log.Error("UI diagnostics: PageHost is null");
            return;
        }

        Log.Information(
            "UI diagnostics: PageHost.Content={ContentType}, Size={Width}x{Height}, ChildCount={ChildCount}",
            pageHost.Content?.GetType().FullName ?? "NULL — no view hosted",
            pageHost.ActualWidth,
            pageHost.ActualHeight,
            pageHost.Content is DependencyObject dep ? VisualTreeHelperGetChildren(dep) : 0);
    }

    public static void LogShellWindowState(
        ShellWindowViewModel? shellViewModel,
        ContentControl? pageHost)
    {
        Log.Information(
            "UI diagnostics: Shell.ViewModel type={ShellVmType}, CurrentViewModel type={CurrentVmType}",
            shellViewModel?.GetType().FullName ?? "null",
            shellViewModel?.CurrentViewModel?.GetType().FullName ?? "null");

        LogPageHostState(pageHost);
    }

    private static int VisualTreeHelperGetChildren(DependencyObject element)
    {
        try
        {
            return System.Windows.Media.VisualTreeHelper.GetChildrenCount(element);
        }
        catch
        {
            return -1;
        }
    }

    private static void ProbeSplatRegistration<TViewModel>()
        where TViewModel : class
    {
        var viewForType = typeof(IViewFor<>).MakeGenericType(typeof(TViewModel));

        try
        {
            var registration = Locator.Current.GetService(viewForType, null);
            if (registration is null)
            {
                Log.Warning(
                    "UI diagnostics: Splat IViewFor<{ViewModel}> is NOT registered",
                    typeof(TViewModel).Name);
                return;
            }

            Log.Information(
                "UI diagnostics: Splat IViewFor<{ViewModel}> registered as {RegistrationType}",
                typeof(TViewModel).Name,
                registration.GetType().FullName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "UI diagnostics: Splat lookup failed for IViewFor<{ViewModel}>", typeof(TViewModel).Name);
        }
    }
}
