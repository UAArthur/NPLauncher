using System.Net.Http;
using System.Text.Json.Nodes;
using BPLauncher.Config;
using BPLauncher.utils;

namespace BPLauncher.services;

public class ApiService
{
    private static readonly HttpClient HttpClient = new();

    //Check for updates

    //Request launch code
    public static async Task<string?> RequestLaunchCode(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.RequestLaunchCode);
        request.Headers.Add("Authorization", $"Bearer {token}");
        Logger.Debug("Requesting launch code...");

        var response = await HttpClient.SendAsync(request);
        Logger.Debug("Launch code requested. Response: " + response.StatusCode);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(result);
            return json?["code"]?.ToString();
        }

        return null;
    }
}