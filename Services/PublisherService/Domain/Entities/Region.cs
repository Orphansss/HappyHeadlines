using System.Text.Json.Serialization;

namespace PublisherService.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Region
{
    Africa,
    Asia,
    Europe,
    NorthAmerica,
    SouthAmerica,
    Oceania,
    Antarctica,
    Global
}