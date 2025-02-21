using System.Diagnostics;
using System.IO;
using BPLauncher.Config;
using BPLauncher.utils;

namespace BPLauncher.services;

public class GameService
{
    public GameService()
    {
        //Create Game Directory if not exists
        if (!Directory.Exists(AppSettings.GetGamePath()))
        {
            Directory.CreateDirectory(AppSettings.GetGamePath());
            Logger.Debug("Created Directory: " + AppSettings.GetGamePath());
        }
    }

    public Process? StartGame(string? args = null)
    {
        Logger.Info(
            "========================================== Start Game ===============================================");
        if (!File.Exists(AppSettings.GetGameExecutablePath()))
        {
            Logger.Error("Game executable not found.");
            return null;
        }

        if (IsGameRunning())
        {
            Logger.Debug("Game is already running.");
            return null;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = AppSettings.GetGameExecutablePath(),
            UseShellExecute = true
        };

        //Adds code to launch arguments -code=ApiService.RequestLaunchCode()
        args += args ??
                $" -code={ApiService.RequestLaunchCode(AppSettings.GetAccounts()!.Accounts.First().Value.Token.AccessToken!).Result}";

        if (!string.IsNullOrEmpty(args))
        {
            startInfo.Arguments = args;
            Logger.Debug("Starting Game with arguments: " + args);
        }

        var gameProcess = Process.Start(startInfo);

        if (gameProcess != null)
        {
            Logger.Debug("Game started.");
            gameProcess.EnableRaisingEvents = true;
        }

        return gameProcess;
    }


    //Kill Game
    public void StopGame()
    {
        //Check if the game is running
        if (!Process.GetProcessesByName("BLUEPROTOCOL-Win64-Shipping").Any())
        {
            Logger.Debug("Game is not running.");
            return;
        }

        //Stop the game
        foreach (var process in Process.GetProcessesByName("BLUEPROTOCOL-Win64-Shipping")) process.Kill();
        Logger.Debug("Game stopped.");
    }

    public bool IsGameRunning()
    {
        return Process.GetProcessesByName("BLUEPROTOCOL-Win64-Shipping").Any() ||
               Process.GetProcessesByName("BLUE PROTOCOL").Any();
    }
}