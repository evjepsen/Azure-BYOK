using System.Text.Json;

namespace Infrastructure.Helpers;

public static class TokenHelper
{
    public static string SerializeJsonObject<T>(T jsonObject) 
    {
        return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
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