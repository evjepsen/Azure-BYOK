using System.Text.Encodings.Web;
using System.Text.Json;

namespace Infrastructure.Helpers;

public static class TokenHelper
{
    public static string SerializeObject<T>(T jsonObject) 
    {
        return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

    public static string SerializeObjectForAzureSignature<T>(T jsonObject) 
    {
        return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // UnsafeRelaxedJsonEscaping is used to avoid the conversion of "+" to "\u002B"
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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