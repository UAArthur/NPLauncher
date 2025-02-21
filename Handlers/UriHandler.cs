using BPLauncher.services;
using BPLauncher.utils;

namespace BPLauncher.Handlers;

public class UriHandlers
{
    public static void Handle(string uri)
    {
        Logger.Debug($"Received URI: {uri}");

        string[] args = uri.Split('/');
        Logger.Debug($"Arguments: {string.Join(", ", args)}");

        if (args.Length > 2 && args[2] == "startGame")
        {
            string[] gameArgs = args.Skip(3).ToArray();
            var gameArgsString = string.Join(" ", gameArgs);

            Logger.Debug($"Launching 'Blue Protocol' with arguments: {gameArgsString}");

            var gameService = new GameService();
            var gameProcess = gameService.StartGame(gameArgsString);
        }
    }
}