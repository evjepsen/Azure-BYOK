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
}