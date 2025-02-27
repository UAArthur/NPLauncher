using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using BPLauncher.Config;
using BPLauncher.utils;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using static System.Diagnostics.Process;

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

    public static Process? StartGame(string? args = null)
    {
        Logger.Info("====================================== Start Game ===========================================");
        if (!File.Exists(AppSettings.GetGameExecutablePath()))
        {
            Logger.Error("Path: " + AppSettings.GetGameExecutablePath());
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
        //-SBExtraGameIni=../ExtraConfig/main_dev.ini
        args += args ??
                $" -code={ApiService.RequestLaunchCode(AppSettings.GetAccounts()!.Accounts.First().Value.Token.AccessToken!).Result} -SBExtraGameIni=../ExtraConfig/main_dev.ini";

        if (!string.IsNullOrEmpty(args))
        {
            startInfo.Arguments = args;
            Logger.Debug("Starting Game with arguments: " + args);
        }

        var gameProcess = Start(startInfo);

        if (gameProcess == null) return gameProcess;
        Logger.Debug("Game started.");
        gameProcess.EnableRaisingEvents = true;

        return gameProcess;
    }


    //Kill Game
    public static void StopGame()
    {
        //Check if the game is running
        if (GetProcessesByName("BLUEPROTOCOL-Win64-Shipping").Length == 0)
        {
            Logger.Debug("Game is not running.");
            return;
        }

        //Stop the game
        foreach (var process in GetProcessesByName("BLUEPROTOCOL-Win64-Shipping")) process.Kill();
        Logger.Debug("Game stopped.");
    }


    public static async Task ImportGameAsync(string zipFilePath)
    {
        var extractPath = AppSettings.GetGamePath();

        using var archive = ZipFile.OpenRead(zipFilePath);
        var totalFiles = archive.Entries.Count;
        var totalBytes = archive.Entries.Sum(entry => entry.Length); // Total bytes to extract
        long extractedBytes = 0;

        var stopwatch = Stopwatch.StartNew();

        await Task.Run(() =>
        {
            Parallel.ForEach(archive.Entries, entry =>
            {
                var destinationPath =
                    Path.Combine(extractPath, entry.FullName.Replace('/', Path.DirectorySeparatorChar));

                if (entry.Length == 0 || entry.FullName.EndsWith($"/"))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);

                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                        FileShare.None, 4096, true);

                    var buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        lock (stopwatch) extractedBytes += bytesRead;
                    }
                }

                // Logging progress
                var progress = (double)extractedBytes / totalBytes * 100;
                var speed = extractedBytes / (stopwatch.Elapsed.TotalSeconds + 0.01) / (1024 * 1024);
                var eta = (totalBytes - extractedBytes) / ((speed + 0.01) * 1024 * 1024);

                Console.WriteLine($"Progress: {progress:F2}% | Speed: {speed:F2} MB/s | ETA: {eta:F1} sec");
            });
        });

        stopwatch.Stop();
        Console.WriteLine($"Extraction completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds!");
    }

    //For testing purposes only!!!!
    //Download Game from the server
    public static async Task DownloadGameFilesAsync()
    {
        const string manifestUrl = "http://192.168.0.180:8080/api/manifest/latest";
        var gamePath = AppSettings.GetGamePath();

        try
        {
            using var httpClient = new HttpClient();

            // Fetch manifest
            var manifestResponse = await httpClient.GetStringAsync(manifestUrl);
            var manifest = JsonConvert.DeserializeObject<Manifest>(manifestResponse);
            if (manifest == null)
            {
                Console.WriteLine("Failed to parse manifest.");
                return;
            }

            Console.WriteLine($"Downloading version {manifest.Version}");

            // Ensure game directory exists
            Directory.CreateDirectory(gamePath);

            var progress = new ConcurrentDictionary<string, long>();
            var stopwatch = Stopwatch.StartNew();

            // Download files in parallel
            await Parallel.ForEachAsync(manifest.Files, async (file, _) =>
            {
                var filePath = Path.Combine(gamePath, file.Key);
                var fileUrl = $"{manifest.BaseUrl.TrimEnd('/')}/{file.Key.TrimStart('/')}";
                var directoryPath = Path.GetDirectoryName(filePath);

                if (directoryPath != null)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Check if file already exists and verify hash
                if (File.Exists(filePath) && VerifyFileHash(filePath, file.Value))
                {
                    Console.WriteLine($"Skipping {file.Key}, already up to date.");
                    return;
                }

                // Download the file if missing or hash mismatch
                await DownloadFileAsync(fileUrl, filePath, progress);
            });

            stopwatch.Stop();
            Console.WriteLine($"Download completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

            // Verify downloaded files
            if (!VerifyInstallation())
            {
                Console.WriteLine("Some files are missing or corrupted. Consider re-downloading.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading game files: {ex.Message}");
        }
    }

    private static async Task DownloadFileAsync(string url, string destination,
        ConcurrentDictionary<string, long> progress)
    {
        Console.WriteLine("URL: " + url);
        try
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None,
                81920, true);
            await using var httpStream = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[81920];
            int bytesRead;
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;

            while ((bytesRead = await httpStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                downloadedBytes += bytesRead;
                progress[destination] = downloadedBytes;

                Console.WriteLine(
                    $"{Path.GetFileName(destination)}: {downloadedBytes / (1024.0 * 1024):F2} MB downloaded");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download {url}: {ex.Message}");
        }
    }

    public static bool VerifyInstallation()
    {
        string manifestUrl = "http://192.168.0.180:8080/api/manifest/latest";
        string gamePath = AppSettings.GetGamePath();

        try
        {
            using var httpClient = new HttpClient();

            // Fetch manifest
            var manifestResponse = httpClient.GetStringAsync(manifestUrl).Result;
            var manifest = JsonConvert.DeserializeObject<Manifest>(manifestResponse);
            if (manifest == null)
            {
                Console.WriteLine("Failed to parse manifest.");
                return false;
            }

            Console.WriteLine($"Verifying installation for version {manifest.Version}");

            bool allFilesValid = true;
            var filesOnDisk = new HashSet<string>(
                Directory.GetFiles(gamePath, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(gamePath, f).Replace("\\", "/"))
            );

            foreach (var file in manifest.Files)
            {
                var filePath = Path.Combine(gamePath, file.Key);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Missing file: {file.Key}");
                    allFilesValid = false;
                    continue;
                }

                if (!VerifyFileHash(filePath, file.Value))
                {
                    Console.WriteLine($"Corrupt file: {file.Key} (hash mismatch)");
                    allFilesValid = false;
                }

                filesOnDisk.Remove(file.Key);
            }

            // Detect extra files that are not in the manifest
            if (filesOnDisk.Count > 0)
            {
                Console.WriteLine("Extra files found:");
                foreach (var extraFile in filesOnDisk)
                {
                    Console.WriteLine($" - {extraFile}");
                }

                allFilesValid = false;
            }

            Console.WriteLine(allFilesValid ? "Installation verified successfully." : "Installation has issues.");
            return allFilesValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying installation: {ex.Message}");
            return false;
        }
    }


    private static bool VerifyFileHash(string filePath, string expectedHash)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var fileHash = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToLower();

        return fileHash == expectedHash;
    }

    public static bool IsGameRunning()
    {
        return GetProcessesByName("BLUEPROTOCOL-Win64-Shipping").Length != 0 || Array.Empty<Process>().Length != 0;
    }

    public static bool IsGameInstalled()
    {
        return File.Exists(AppSettings.GetGameExecutablePath());
    }
}

public class Manifest
{
    public string Version { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Files { get; set; } = new();
}