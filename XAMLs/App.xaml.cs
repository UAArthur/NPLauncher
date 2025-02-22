using System.Windows;
using BPLauncher.Config;
using BPLauncher.Handlers;
using BPLauncher.Helpers;
using BPLauncher.utils;
using Hardcodet.Wpf.TaskbarNotification;

namespace BPLauncher.XAMLs;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private static Mutex? _mutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Check if Launcher is already running
        const string appName = "BPLauncher";
        _mutex = new Mutex(true, appName, out var createdNew);
        if (!createdNew)
        {
            Current.Shutdown();
            return;
        }
       
        // Init AppSettings
        var appSettings = new AppSettings();
        await LanguageHandler.LoadLanguagesAsync();

        // Initialize Logger and Console
        ConsoleHelper.ShowConsole();
        Logger.Debug("Console initialized.");

        // Initialize Tray Icon
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.TrayLeftMouseDown += TrayIcon_Click;
        _trayIcon.TrayMouseDoubleClick += TrayIcon_Click;
        Logger.Info("Application started, tray icon initialized.");

        // Register Protocol
        if (LauncherRegistryHandler.IsFirstStart())
        {
            LauncherRegistryHandler.RegisterProtocol();
            LauncherRegistryHandler.SetFirstStartDone();
        }

        Logger.Info("Loading accounts...");
        await appSettings.LoadAccountsAsync();

        if (AppSettings.GetAccounts()!.Accounts.Count == 0)
        {
            Logger.Debug("No valid account found, opening AuthWindow.");
            Current.MainWindow = new AuthWindow();
        }
        else
        {
            Logger.Debug("Valid account found, opening MainWindow.");
            Current.MainWindow = new MainWindow();
        }

        Current.MainWindow.Show();

        // Handle URI
        if (e.Args.Length > 0)
        {
            var combinedArgs = string.Join(" ", e.Args);
            Logger.Info($"Combined Arguments: {combinedArgs}");
            UriHandlers.Handle(combinedArgs);
        }
    }

    private void TrayIcon_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("Tray icon clicked.");

        // Check if any window is already open
        var activeWindow = Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

        if (activeWindow != null)
        {
            Logger.Debug($"Focusing existing window: {activeWindow.Title}");
            activeWindow.WindowState = WindowState.Normal;
            activeWindow.ShowInTaskbar = true;
            activeWindow.Activate();
            return;
        }

        Logger.Debug("No visible window found, deciding which window to open.");

        if (Current.MainWindow == null || Current.MainWindow is AuthWindow)
        {
            Logger.Debug("Opening MainWindow.");
            Current.MainWindow = new MainWindow();
        }

        Current.MainWindow.Show();
        Current.MainWindow.WindowState = WindowState.Normal;
        Current.MainWindow.ShowInTaskbar = true;
        Current.MainWindow.Activate();

        Logger.Debug("MainWindow displayed.");
    }

    private void TrayIcon_Exit_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("Exit clicked, shutting down app.");
        _trayIcon?.Dispose();
        Current.Shutdown();
    }

    private void TrayIcon_Open_Click(object sender, RoutedEventArgs e)
    {
        var activeWindow = Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

        if (activeWindow != null)
        {
            activeWindow.WindowState = WindowState.Normal;
            activeWindow.ShowInTaskbar = true;
            activeWindow.Activate();
        }
    }
}