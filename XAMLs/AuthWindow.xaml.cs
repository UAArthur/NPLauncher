using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BPLauncher.Config;
using BPLauncher.services;
using BPLauncher.utils;

namespace BPLauncher.XAMLs;

public partial class AuthWindow : Window
{
    private readonly AuthService _authService = new();

    public AuthWindow()
    {
        InitializeComponent();
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


    private void UsernameBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (UsernameBox.Text == "Username")
        {
            UsernameBox.Text = "";
            UsernameBox.Foreground = Brushes.White;
        }
    }

    private void UsernameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameBox.Text))
        {
            UsernameBox.Text = "Username";
            UsernameBox.Foreground = Brushes.Gray;
        }
    }

    private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
        PasswordHint.Visibility = Visibility.Collapsed;
        PasswordBox.Foreground = Brushes.White;
    }

    private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            PasswordHint.Visibility = Visibility.Visible;
            PasswordBox.Foreground = Brushes.Gray;
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordHint.Visibility =
            string.IsNullOrWhiteSpace(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        PasswordBox.Foreground = Brushes.White;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text;
        var password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Logger.Error("Missing fields");
            MessageBox.Show("Please fill in all fields", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Logger.Info("Trying to login");
        var success = await _authService.Login(username, password);

        if (success)
        {
            Logger.Info("Login successful");
            // Save the account
            AppSettings.GetAccounts()
                ?.AddOrUpdateAccountAsync(_authService.GetCurrentAccount() ?? throw new InvalidOperationException());
            AppSettings.GetAccounts()?.SaveAsync();

            // Open the MainWindow
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Close the LoginWindow
            Application.Current.Windows[0]?.Close();
        }
        else
        {
            MessageBox.Show("Login failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Error("Login failed");
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}