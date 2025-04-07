using System.Security.Cryptography.X509Certificates;
using Infrastructure.Interfaces;

namespace Infrastructure;

public class CertificateCache : ICertificateCache
{
    private X509Certificate2? _certificate;

    public CertificateCache()
    {
        _certificate = null;
    }

    public X509Certificate2? GetCertificate()
    {
        return _certificate;
    }

    public void AddCertificate(X509Certificate2 certificate)
    {
        var isCertificateValid = ValidateCertificate(certificate);

        if (isCertificateValid)
        {
            _certificate = certificate;
        } else 
        {
            throw new InvalidOperationException("Invalid certificate.");
        }
    }

    public bool ValidateCertificate(X509Certificate2 certificate)
    {
        // Dummy implementation for the prototype
        // In a real implementation, you would check the certificate's validity
        return true;
    }
}