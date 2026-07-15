using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TeslaDash.Services;

public sealed class TeslaFleetClient(HttpClient httpClient, TeslaOAuthClient oauthClient, IOptions<TeslaOptions> options)
{
    private readonly TeslaOptions _options = options.Value;

    public async Task<IReadOnlyList<TeslaVehicle>> ListVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var token = await oauthClient.GetValidTokenAsync(cancellationToken) ?? throw new InvalidOperationException("Tesla is not connected.");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.FleetApiBaseUrl.TrimEnd('/')}/api/1/vehicles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Tesla Fleet API failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
        return (await response.Content.ReadFromJsonAsync<TeslaVehicleList>(cancellationToken))?.Response ?? [];
    }
}

public sealed record TeslaVehicle([property: JsonPropertyName("id")] long Id, [property: JsonPropertyName("vin")] string Vin,
    [property: JsonPropertyName("display_name")] string DisplayName, [property: JsonPropertyName("state")] string State);
public sealed record TeslaVehicleList([property: JsonPropertyName("response")] List<TeslaVehicle> Response);
