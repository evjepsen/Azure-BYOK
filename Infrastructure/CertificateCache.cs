using System.Security.Cryptography.X509Certificates;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class CertificateCache : ICertificateCache
{
    private readonly ReaderWriterLockSlim _certificateLock = new();
    private X509Certificate2? _certificate;
    private readonly ILogger<CertificateCache> _logger;
    private readonly ApplicationOptions _applicationOption;

    public CertificateCache(ILoggerFactory loggerFactory, IOptions<ApplicationOptions> applicationOption)
    {
        _certificate = null;
        _logger = loggerFactory.CreateLogger<CertificateCache>();
        _applicationOption = applicationOption.Value;
    }

    public X509Certificate2? GetCertificate()
    {
        try
        {
            _certificateLock.EnterReadLock();
            _logger.LogInformation("Getting the certificate from the cache {cert}", _certificate);
            return _certificate;
        }
        finally
        {
            _certificateLock.ExitReadLock();
        }
        
    }

    public void AddCertificate(X509Certificate2 certificate)
    {
        try
        {
            _certificateLock.EnterWriteLock();

            var isCertificateValid = ValidateCertificate(certificate);

            if (isCertificateValid)
            {
                _logger.LogInformation("Adding the certificate to the cache");
                _certificate?.Dispose();
                _certificate = new X509Certificate2(certificate);
            }
            else
            {
                throw new InvalidOperationException("Invalid certificate.");
            }
        }
        finally
        {
            _certificateLock.ExitWriteLock();
        }
        
    }

    public bool ValidateCertificate(X509Certificate2 certificate)
    {
        // Dummy implementation for the prototype
        // In a real implementation, you would check the certificate's validity
        // That it is not expired, revoked, etc.
        // And signed by a trusted CA and so on
        
        // Check that it is still valid
        _logger.LogInformation("Validating the certificate");
        if (DateTime.Now > certificate.NotAfter || DateTime.Now < certificate.NotBefore)
        {
            _logger.LogInformation("The certificate is expired or not yet valid");
            return false;
        }
        
        // Check that the issuer is the specified issuer
        var subject = certificate.Subject;
        if (!subject.Equals(_applicationOption.ValidSubject, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("The certificate was not issued to a valid subject");
            return false;
        }
        
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        
        var isValid = chain.Build(certificate);
        if (!isValid)
        {
            _logger.LogInformation("Checking the certificate chain");
            var isProblematicStatus = chain.ChainStatus
                .Any(status => status.Status != X509ChainStatusFlags.UntrustedRoot);
            return !isProblematicStatus;
        }
        
        return true;
    }
}