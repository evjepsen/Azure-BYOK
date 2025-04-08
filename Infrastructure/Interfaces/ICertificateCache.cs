using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.Interfaces;

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
    
    bool ValidateCertificate(X509Certificate2 certificate);
}