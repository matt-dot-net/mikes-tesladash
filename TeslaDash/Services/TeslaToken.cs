using System.Text.Json.Serialization;

namespace TeslaDash.Services;

public sealed record TeslaToken(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("scope")] string? Scope = null,
    DateTimeOffset? ExpiresAt = null)
{
    public TeslaToken WithExpiry() => this with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ExpiresIn) };
    public bool NeedsRefresh => ExpiresAt is null || ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(2);
}
