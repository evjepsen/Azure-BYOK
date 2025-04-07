using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.Interfaces;

public interface ICertificateCache
{
    X509Certificate2 GetCertificate();
    
    void AddCertificate(X509Certificate2 certificate);
    
    bool ValidateCertificate(X509Certificate2 certificate);
}