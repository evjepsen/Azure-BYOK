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
    
    /// <summary>
    /// The key vault does not have soft delete enabled error message
    /// </summary>
    public const string SoftDeleteErrorMessage = "The key vault does not have soft delete enabled";

    /// <summary>
    /// When an unexpected error occurs when getting logs
    /// </summary>
    public const string InternalServerErrorOccuredGettingTheLogs = "An unexpected error occured when getting the logs";

    /// <summary>
    /// When an unexpected error occurs when getting the role assignments
    /// </summary>
    public const string InternalServerErrorOccuredWhenGettingRoleAssignments =
        "An unexpected error occured when getting the role assignments";
    
    /// <summary>
    /// The user has given incorrect input
    /// </summary>
    public const string InvalidInputForRequest = "The request could not be completed - the request body is invalid (Properly JSON formatting error)";
    
    /// <summary>
    /// Certificate is not yet valid
    /// </summary>
    public const string CertificateNotYetValid = "The certificate is not yet valid";
    
    /// <summary>
    /// The certificate has expired
    /// </summary>
    public const string CertificateHasExpired = "The certificate has expired";
    
    /// <summary>
    /// The certificate is not valid
    /// </summary>
    public const string CertificateIsInvalid = "The certificate is invalid";
    
    /// <summary>
    /// The success message when the certificate was uploaded successfully
    /// </summary>
    public const string CertificateWasUploadedSuccessfully = "Certificate was uploaded successfully";
}