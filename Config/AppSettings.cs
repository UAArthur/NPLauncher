﻿using System.IO;
using BPLauncher.services;
using BPLauncher.utils;

namespace BPLauncher.Config;

public class AppSettings
{
    //Default BaseUrl
    private static string BaseUrl { get; } = "http://192.168.0.180:8081";

    //Default Directory Appdata\Local
    private static readonly string DefaultDirectory =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\BPLauncher";

    //Game Path (Default: AppData\Local\BPLauncher\Game\BLUEPROTOCOL)
    private static readonly string _gamePath = GetDefaultDirectory() + "\\Game";

    //Executable Path (Default: AppData\Local\BPLauncher\Game\BLUEPROTOCOL\BLUEPROTOCOL\Binaries\Win64\BLUEPROTOCOL-Win64-Shipping.exe)
    private static readonly string _executablePath =
        _gamePath + @"\BLUEPROTOCOL\Binaries\Win64\BLUEPROTOCOL-Win64-Shipping.exe";

    //Version
    private static readonly string Version = "1.0.0";

    //Accounts
    private static readonly LauncherAccountStore? Accounts = new();

    public AppSettings()
    {
        //Create Directory if not exists
        if (!Directory.Exists(DefaultDirectory))
        {
            Directory.CreateDirectory(DefaultDirectory);
            Logger.Debug("Created Directory: " + DefaultDirectory);
        }
    }

    public async Task LoadAccountsAsync()
    {
        Logger.Debug("Loading accounts asynchronously...");
        await Accounts?.LoadAsync()!;
        if (Accounts?.Accounts.Count > 0)
        {
            AuthService.SetCurrentAccount(GetAccounts()!.Accounts.First().Value);
        }
    }

    public static string GetBaseUrl()
    {
        return BaseUrl;
    }

    public static string GetDefaultDirectory()
    {
        return DefaultDirectory;
    }

    public static string GetVersion()
    {
        return Version;
    }

    public static string GetGamePath()
    {
        return _gamePath;
    }

    public static string GetGameExecutablePath()
    {
        return _executablePath;
    }

    public static LauncherAccountStore? GetAccounts()
    {
        return Accounts;
    }
}