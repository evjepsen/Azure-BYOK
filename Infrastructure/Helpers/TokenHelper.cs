using System.Text.Json;

namespace Infrastructure.Helpers;

public static class TokenHelper
{
    public static string SerializeJsonObject<T>(T jsonObject, JsonNamingPolicy jsonNamingPolicy, bool writeIndented = true) 
    {
        return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNamingPolicy = jsonNamingPolicy
        });
    }

    public static T? DeserializeJsonObject<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
}
}