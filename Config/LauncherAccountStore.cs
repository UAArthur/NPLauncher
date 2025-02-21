using System.IO;
using System.Text.Json;
using BPLauncher.utils;

namespace BPLauncher.Config;

public class LauncherAccountStore
{
    private static readonly string FilePath = AppSettings.GetDefaultDirectory() + "\\accounts.json";

    public Dictionary<Guid, Account> Accounts { get; private set; } = new();

    public LauncherAccountStore()
    {
        Logger.Debug("Initializing LauncherAccountStore...");
    }

    public async Task LoadAsync()
    {
        if (File.Exists(FilePath))
            try
            {
                Logger.Debug("Loading accounts from file...");
                var json = await File.ReadAllTextAsync(FilePath);

                var tempData = JsonSerializer.Deserialize<LauncherAccountStoreWrapper>(json);

                if (tempData?.Accounts != null)
                {
                    Accounts = tempData.Accounts.ToDictionary(
                        kvp => Guid.Parse(kvp.Key),
                        kvp => kvp.Value
                    );

                    Logger.Info("Loaded " + Accounts.Count + " accounts.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load accounts: " + ex.Message);
            }
        else
            Logger.Debug("No existing account file found.");
    }

    private class LauncherAccountStoreWrapper
    {
        public Dictionary<string, Account>? Accounts { get; set; }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
            Logger.Debug("Accounts saved successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save accounts: " + ex.Message);
        }
    }

    public async Task AddOrUpdateAccountAsync(Account account)
    {
        Accounts[account.Profile.Uuid] = account;
        await SaveAsync();
    }

    public bool TryGetAccount(Guid uuid, out Account account)
    {
        return Accounts.TryGetValue(uuid, out account!);
    }
}

public class Account
{
    public Token Token { get; set; } = new();
    public Profile Profile { get; set; } = new();
}

public class Token
{
    public string? AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
}

public class Profile
{
    public Guid Uuid { get; set; }
    public string Username { get; set; } = string.Empty;
}