using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum Urgency { Low, Moderate, High, Critical }

public record RescueCaseResponse(Guid Id, Urgency Urgency);

class Program {
    static void Main() {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            Converters = { new JsonStringEnumConverter() }
        };
        var json = "{\"id\":\"c04b3e83-380d-4034-934d-17631bd28ba7\",\"urgency\":\"Moderate\"}";
        try {
            var obj = JsonSerializer.Deserialize<RescueCaseResponse>(json, options);
            Console.WriteLine(obj.Urgency);
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}
