using System.Text.Json.Serialization;

namespace NeoBank.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Currency
{
    UAH = 980,  // ISO 4217 numeric codes
    USD = 840,
    EUR = 978
}
