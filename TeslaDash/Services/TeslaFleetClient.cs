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

    public async Task<TeslaVehicleData> GetVehicleDataAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        var token = await oauthClient.GetValidTokenAsync(cancellationToken) ?? throw new InvalidOperationException("Tesla is not connected.");
        var baseUrl = _options.FleetApiBaseUrl.TrimEnd('/');
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/api/1/vehicles/{vehicleId}/vehicle_data?endpoints=charge_state;vehicle_state");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Tesla Fleet API failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
        return (await response.Content.ReadFromJsonAsync<TeslaVehicleDataEnvelope>(cancellationToken))?.Response
            ?? throw new HttpRequestException("Tesla Fleet API returned no vehicle data.");
    }

    public async Task RemoteBoomboxAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        var token = await oauthClient.GetValidTokenAsync(cancellationToken) ?? throw new InvalidOperationException("Tesla is not connected.");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.FleetApiBaseUrl.TrimEnd('/')}/api/1/vehicles/{vehicleId}/command/remote_boombox")
        {
            Content = JsonContent.Create(new { sound = 0 })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Tesla Fleet API failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");

        var result = await response.Content.ReadFromJsonAsync<TeslaCommandEnvelope>(cancellationToken);
        if (result?.Response.Result != true)
            throw new InvalidOperationException(result?.Response.Reason ?? "Tesla rejected the Remote Boombox command.");
    }
}

public sealed record TeslaVehicle([property: JsonPropertyName("id")] long Id, [property: JsonPropertyName("vin")] string Vin,
    [property: JsonPropertyName("display_name")] string DisplayName, [property: JsonPropertyName("state")] string State);
public sealed record TeslaVehicleList([property: JsonPropertyName("response")] List<TeslaVehicle> Response);
public sealed record TeslaVehicleDataEnvelope([property: JsonPropertyName("response")] TeslaVehicleData Response);
public sealed record TeslaVehicleData(
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("charge_state")] TeslaChargeState? ChargeState,
    [property: JsonPropertyName("vehicle_state")] TeslaVehicleState? VehicleState);
public sealed record TeslaChargeState(
    [property: JsonPropertyName("battery_level")] int BatteryLevel,
    [property: JsonPropertyName("battery_range")] double BatteryRange,
    [property: JsonPropertyName("charge_limit_soc")] int ChargeLimitSoc,
    [property: JsonPropertyName("charging_state")] string? ChargingState,
    [property: JsonPropertyName("charge_energy_added")] double ChargeEnergyAdded,
    [property: JsonPropertyName("charger_power")] int ChargerPower,
    [property: JsonPropertyName("minutes_to_full_charge")] int MinutesToFullCharge);
public sealed record TeslaVehicleState(
    [property: JsonPropertyName("car_version")] string? CarVersion,
    [property: JsonPropertyName("odometer")] double Odometer);
public sealed record TeslaCommandEnvelope([property: JsonPropertyName("response")] TeslaCommandResponse Response);
public sealed record TeslaCommandResponse(
    [property: JsonPropertyName("result")] bool Result,
    [property: JsonPropertyName("reason")] string? Reason);
