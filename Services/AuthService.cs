using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using BPLauncher.Config;
using BPLauncher.Responses;
using BPLauncher.utils;
using Microsoft.VisualBasic.Logging;

namespace BPLauncher.services;

public class AuthService
{
    private readonly HttpClient _httpClient = new();

    private static Account? _currentAccount;

    public async Task<bool> Login(string username, string password)
    {
        var data = new { username, password };
        var response = await _httpClient.PostAsJsonAsync(Endpoints.Login, data);

        if (!response.IsSuccessStatusCode) return false;
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result == null) return false;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);
        var profile = new Profile
        {
            Uuid = Guid.Parse(jwtToken.Claims.First(claim => claim.Type == "sub").Value),
            Username = jwtToken.Claims.First(claim => claim.Type == "username").Value
        };

        var expClaim = jwtToken.Claims.First(claim => claim.Type == "exp").Value;
        var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime;

        var token = new Token
        {
            AccessToken = result.Token,
            AccessTokenExpiresAt = exp
        };

        _currentAccount = new Account
        {
            Profile = profile,
            Token = token
        };

        return true;

    }
    
    public async Task<bool> CheckToken()
    {
        Logger.Info("Checking if token is valid...");
        if (_currentAccount == null) return false;
        if (_currentAccount.Token.AccessTokenExpiresAt <= DateTime.UtcNow) return false;
        var response = await _httpClient.PostAsJsonAsync(Endpoints.ValidateToken, new { token = _currentAccount.Token.AccessToken });
        return response.IsSuccessStatusCode;
    }

    public static Account? GetCurrentAccount()
    {
        return _currentAccount;
    }
    
    public static void SetCurrentAccount(Account account)
    {
        _currentAccount = account;
    }
}