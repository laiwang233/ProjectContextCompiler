using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pcc.Infrastructure.Json;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public static IReadOnlyList<string> DeserializeStringList(string json)
    {
        return Deserialize<IReadOnlyList<string>>(json) ?? [];
    }
}
