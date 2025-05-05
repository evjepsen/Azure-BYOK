namespace API.Models;

/// <summary>
/// The result the callback method returns
/// </summary>
public class CallbackResult
{
    /// <summary>
    /// The access token to be used in subsequent API requests
    /// </summary>
    public required string AccessToken { get; set; }
}