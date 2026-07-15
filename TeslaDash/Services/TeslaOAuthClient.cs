using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace TeslaDash.Services;

public sealed class TeslaOAuthClient(HttpClient httpClient, IOptions<TeslaOptions> options, ITeslaTokenStore tokenStore)
{
    private const string AuthorizeUrl = "https://auth.tesla.com/oauth2/v3/authorize";
    private const string TokenUrl = "https://fleet-auth.prd.vn.cloud.tesla.com/oauth2/v3/token";
    private readonly TeslaOptions _options = options.Value;

    public string CreateAuthorizationUrl(string state) => QueryHelpers.AddQueryString(AuthorizeUrl, new Dictionary<string, string?>
    {
        ["response_type"] = "code", ["client_id"] = _options.ClientId,
        ["redirect_uri"] = _options.RedirectUri, ["scope"] = _options.Scopes,
        ["state"] = state, ["prompt_missing_scopes"] = "true"
    });

    public static string CreateState() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public async Task<TeslaToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret, ["code"] = code,
            ["audience"] = _options.FleetApiBaseUrl, ["redirect_uri"] = _options.RedirectUri
        };
        using var response = await httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(values), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var token = (await response.Content.ReadFromJsonAsync<TeslaToken>(cancellationToken))!.WithExpiry();
        await tokenStore.SaveAsync(token, cancellationToken);
        return token;
    }

    public async Task<TeslaToken?> GetValidTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await tokenStore.GetAsync(cancellationToken);
        if (token is null || !token.NeedsRefresh) return token;
        var values = new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["client_id"] = _options.ClientId, ["refresh_token"] = token.RefreshToken };
        using var response = await httpClient.PostAsync(TokenUrl, new FormUrlEncodedContent(values), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var refreshed = (await response.Content.ReadFromJsonAsync<TeslaToken>(cancellationToken))!.WithExpiry();
        await tokenStore.SaveAsync(refreshed, cancellationToken);
        return refreshed;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Tesla authentication failed ({(int)response.StatusCode}): {body}");
    }
}
