using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.Interfaces;

/// <summary>
/// Interface for the certificate cache
/// </summary>
public interface ICertificateCache
{
    /// <summary>
    /// Get the customer uploaded certificate
    /// </summary>
    /// <returns>The customer's certificate</returns>
    X509Certificate2? GetCertificate();
    
    /// <summary>
    /// Add the customer uploaded certificate to the cache
    /// </summary>
    /// <param name="certificate">The customers certificate</param>
    void AddCertificate(X509Certificate2 certificate);
    
    /// <summary>
    /// Used to validate a certificate
    /// </summary>
    /// <param name="certificate">The certificate to validate</param>
    /// <returns>True if the certificate is valid</returns>
    bool ValidateCertificate(X509Certificate2 certificate);
}