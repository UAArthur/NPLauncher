namespace BPLauncher.Config;

public static class Endpoints
{
    public static string CheckForUpdates = $"{AppSettings.GetBaseUrl()}/updates/check";
    //Auth
    public static string Login = $"{AppSettings.GetBaseUrl()}/auth/login";
    public static string Register = $"{AppSettings.GetBaseUrl()}/auth/register";
    public static string ValidateToken = $"{AppSettings.GetBaseUrl()}/auth/validate";
    public static string RequestLaunchCode = $"{AppSettings.GetBaseUrl()}/api/v1/code";
}