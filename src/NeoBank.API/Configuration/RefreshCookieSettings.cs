namespace NeoBank.API.Configuration;

public class RefreshCookieSettings
{
    public string Name { get; init; } = "neobank_refresh";
    public string SameSite { get; init; } = "Strict";
    public bool Secure { get; init; } = true;
    public string Path { get; init; } = "/api/v1/auth";
}