using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BPLauncher.Config;
using BPLauncher.services;
using BPLauncher.utils;
using BPLauncher.XAMLs.UC;

namespace BPLauncher.XAMLs;

public partial class MainWindow : Window
{
    private readonly GameService _gameService;
    private readonly DispatcherTimer _gameStatusTimer;

    public MainWindow()
    {
        InitializeComponent();
        // Set Version
        VersionLabel.Content = "Version: " + AppSettings.GetVersion();
        _gameService = new GameService();

        // Initialize and start the timer
        _gameStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _gameStatusTimer.Tick += GameStatusTimer_Tick;
        _gameStatusTimer.Start();
        GameStatusTimer_Tick(null, null);
    }

    private void GameStatusTimer_Tick(object? sender, EventArgs? e)
    {
        if (_gameService.IsGameRunning())
        {
            StartButton.IsEnabled = false;
            StartButton.Content = "Game Running";
        }
        else
        {
            StartButton.IsEnabled = true;
            StartButton.Content = "Start Game";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            Logger.Info("Trying to start the game...");
            WindowState = WindowState.Minimized;

            // Start the game
            var gameProcess = await Task.Run(() => _gameService.StartGame());
            if (gameProcess != null)
            {
                gameProcess.EnableRaisingEvents = true;
                gameProcess.Exited += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Logger.Info(
                            "========================================== Game Exited ===============================================");
                        WindowState = WindowState.Normal;
                        Show();
                    });
                };
            }
        }
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsMenuContainer.Visibility == Visibility.Visible)
        {
            SettingsMenuContainer.Visibility = Visibility.Collapsed;
            SettingsMenuContainer.IsHitTestVisible = false;
            return;
        }

        SettingsMenuContainer.Children.Clear();

        var settingsMenu = new SettingsMenu();
        SettingsMenuContainer.Children.Add(settingsMenu);

        settingsMenu.Arrange(new Rect(settingsMenu.DesiredSize));

        var buttonPosition = SettingsButton.TransformToAncestor(MainGrid)
            .Transform(new Point(0, 0));

        var menuHeight = settingsMenu.ActualHeight > 0 ? settingsMenu.ActualHeight : 250;
        var menuWidth = settingsMenu.ActualWidth > 0 ? settingsMenu.ActualWidth : 250;

        double offsetX = 30;
        var centeredX = buttonPosition.X + SettingsButton.ActualWidth / 2 - menuWidth / 2 - offsetX;

        settingsMenu.Margin = new Thickness(centeredX, buttonPosition.Y - menuHeight - 100, 0, 0);

        SettingsMenuContainer.Visibility = Visibility.Visible;
        settingsMenu.IsHitTestVisible = true;
        SettingsMenuContainer.IsHitTestVisible = false;
    }


    /////////////////// Event Handlers ///////////////////
    private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (SettingsMenuContainer.Visibility == Visibility.Visible)
        {
            Logger.Info("Clicked outside of SettingsMenu. Closing.");

            if (!SettingsMenuContainer.IsAncestorOf((DependencyObject)e.OriginalSource))
            {
                Logger.Info("Clicked outside of SettingsMenu. Closing.");
                SettingsMenuContainer.Visibility = Visibility.Collapsed;
                SettingsMenuContainer.IsHitTestVisible = false;
            }
        }
    }


    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}