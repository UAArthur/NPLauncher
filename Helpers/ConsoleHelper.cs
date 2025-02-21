using System.IO;
using System.Runtime.InteropServices;

namespace BPLauncher.Helpers;

public static class ConsoleHelper
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    private static bool _consoleAllocated;

    public static void ShowConsole()
    {
        if (!_consoleAllocated)
        {
            AllocConsole();
            _consoleAllocated = true;
            RedirectOutput();
        }
    }

    public static void HideConsole()
    {
        if (_consoleAllocated)
        {
            FreeConsole();
            _consoleAllocated = false;
        }
    }

    private static void RedirectOutput()
    {
        var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(writer);
        Console.SetError(writer);
    }
}