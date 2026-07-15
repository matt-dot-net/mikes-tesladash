namespace TeslaDash.Services;

public sealed class TeslaOptions
{
    public const string SectionName = "Tesla";
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public string RedirectUri { get; init; } = "http://localhost:5187/Auth/Callback";
    public string FleetApiBaseUrl { get; init; } = "https://fleet-api.prd.na.vn.cloud.tesla.com";
    public string Scopes { get; init; } = "openid offline_access vehicle_device_data";
    public string TokenPath { get; init; } = "data/tesla-tokens.protected";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
