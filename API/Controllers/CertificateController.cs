using System.Security.Cryptography.X509Certificates;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Used to handle the customer supplied certificate
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ShouldBeAllowedEmail")]
[Route("[controller]")]
public class CertificateController : Controller
{
    private readonly ICertificateCache _certificateCache;
    private readonly ILogger<CertificateController> _logger;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="certificateCache">To store the certificate</param>
    /// <param name="loggerFactory">The logger factory for the audit controller</param>
    public CertificateController(ICertificateCache certificateCache, ILoggerFactory loggerFactory)
    {
        _certificateCache = certificateCache;
        _logger = loggerFactory.CreateLogger<CertificateController>();
    }

    /// <summary>
    /// This endpoint is used to upload a customer certificate
    /// </summary>
    /// <param name="certificateFile">A file containing the customer HSM's certificate</param>
    /// <response code="200">If the certificate was uploaded successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    public async Task<IActionResult> UploadCustomerCertificate(IFormFile certificateFile)
    {
        if (certificateFile.Length == 0)
        {
            _logger.LogError("Certificate file is null or empty.");
            return BadRequest("Certificate file is required.");
        }
        
        // Try to read the uploaded file
        byte[] certData;
        try
        {
            using var memoryStream = new MemoryStream();
            await certificateFile.CopyToAsync(memoryStream);
            certData= memoryStream.ToArray();
        }
        catch (Exception e)
        {
            _logger.LogError("Error reading certificate file: {Message}", e.Message);
            return BadRequest("Error reading certificate file.");
        } 
        
        // Try to create a certificate from the uploaded file
        X509Certificate2 certificate;
        try
        {
            certificate = new X509Certificate2(certData);
            
            if (certificate.NotAfter < DateTime.UtcNow)
            {
                return BadRequest("Certificate has expired");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Error creating the certificate: {Message}", e.Message);
            return BadRequest("Error creating the certificate.");
        }

        // Try to add the certificate to the cache
        try
        {
            _certificateCache.AddCertificate(certificate);
        }
        catch (InvalidOperationException)
        {
            _logger.LogError("Certificate was not valid");
            return BadRequest("Certificate was not valid");
        }
        catch (Exception e)
        {
            _logger.LogError("Error adding the certificate to the cache {Message}", e.Message);
            return BadRequest("Error adding the certificate to the cache");
        }
        
        return Ok();
    }
}