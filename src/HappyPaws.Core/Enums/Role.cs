using System.Text.Json.Serialization;

namespace HappyPaws.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    Adopter,
    Foster,
    Transporter,
    Sponsor,
    Veterinarian,
    Admin
}
