using System.IO;
using BPLauncher.Config;

namespace BPLauncher.utils;

public static class Logger
{
    private static readonly string LogDirectory = Path.Combine(AppSettings.GetDefaultDirectory(), "Logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "BPLauncher.log");

    static Logger()
    {
        if (!Directory.Exists(LogDirectory))
            Directory.CreateDirectory(LogDirectory);
    }

    public static void Info(string message)
    {
        Log("INFO", message);
    }

    public static void Warn(string message)
    {
        Log("WARN", message);
    }

    public static void Error(string message)
    {
        Log("ERROR", message);
    }

    public static void Debug(string message)
    {
        Log("DEBUG", message);
    }

    private static void Log(string level, string message)
    {
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        try
        {
            Console.WriteLine(logEntry);
            Console.Out.Flush();

            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[Logger] Failed to write log: {ex.Message}");
        }
    }
}