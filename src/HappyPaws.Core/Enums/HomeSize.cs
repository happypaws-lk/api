using System.Text.Json.Serialization;

namespace HappyPaws.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HomeSize
{
    Apartment,
    House,
    Estate
}
