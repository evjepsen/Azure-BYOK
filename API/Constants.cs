namespace API;

/// <summary>
/// Class of constants used in the API
/// </summary>
public static class Constants
{
    /// <summary>
    /// The Invalid Signature error message
    /// </summary>
    public const string InvalidSignatureErrorMessage = "The signature is invalid";
    
    /// <summary>
    /// The request is no longer valid error message
    /// </summary>
    public const string TheRequestIsNoLongerValid = "The request is no longer valid";

    /// <summary>
    /// The request is missing a key vault alert error message
    /// </summary>
    public const string MissingKeyVaultAlert = "There is no alert for key vault usage";

    /// <summary>
    /// The request is missing an action group error message
    /// </summary>
    public const string MissingActionGroup = "Trying to upload a key without an action group";
}