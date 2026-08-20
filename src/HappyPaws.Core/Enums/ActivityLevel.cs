using System.Text.Json.Serialization;

namespace HappyPaws.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivityLevel
{
    Low,
    Moderate,
    High
}
