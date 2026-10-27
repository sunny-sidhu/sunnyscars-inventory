using System.Text.Json.Serialization;

namespace SunnysCars.Inventory.Api.Models;

/// <summary>
/// Gearbox type of a car. Serialized as the lowercase strings "automatic" and "manual".
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Transmission>))]
public enum Transmission
{
    [JsonStringEnumMemberName("automatic")] Automatic,
    [JsonStringEnumMemberName("manual")] Manual
}
