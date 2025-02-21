using System.Diagnostics;
using Microsoft.Win32;

namespace BPLauncher.Handlers;

public abstract class LauncherRegistryHandler
{
    private const string ProtocolName = "bplauncher";
    private static string? ExecutablePath => Process.GetCurrentProcess().MainModule?.FileName;

    private const string RegistryPath = @"HKEY_CURRENT_USER\Software\BPLauncher";
    private const string FirstRunKey = "FirstRun";

    public static bool IsFirstStart()
    {
        var value = Registry.GetValue(RegistryPath, FirstRunKey, null);
        return value == null;
    }

    public static void SetFirstStartDone()
    {
        Registry.SetValue(RegistryPath, FirstRunKey, "1");
    }


    public static void RegisterProtocol()
    {
        try
        {
            // Check if protocol is already registered
            if (Registry.ClassesRoot.OpenSubKey(ProtocolName) != null)
            {
                Console.WriteLine($"{ProtocolName} protocol is already registered.");
                return;
            }

            // Create protocol registry keys
            using (var key = Registry.ClassesRoot.CreateSubKey(ProtocolName))
            {
                key.SetValue("", "URL:BPLauncher Protocol");
                key.SetValue("URL Protocol", "");
            }

            using (var commandKey = Registry.ClassesRoot.CreateSubKey($@"{ProtocolName}\shell\open\command"))
            {
                commandKey.SetValue("", $"\"{ExecutablePath}\" \"%1\"");
            }

            Console.WriteLine($"{ProtocolName} protocol registered successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering protocol: {ex.Message}");
        }
    }
}