using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using BPLauncher.Config;
using BPLauncher.Responses;

namespace BPLauncher.services;

public class AuthService
{
    private readonly HttpClient _httpClient = new();

    private static Account? _currentAccount;

    public async Task<bool> Login(string username, string password)
    {
        var data = new { username, password };
        var response = await _httpClient.PostAsJsonAsync(Endpoints.Login, data);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (result != null)
            {
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
        }

        return false;
    }

    public Account? GetCurrentAccount()
    {
        return _currentAccount;
    }
}