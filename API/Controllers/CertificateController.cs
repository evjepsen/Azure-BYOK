using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure;
using Infrastructure.Exceptions;
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
    private readonly ISignatureService _signatureService;
    private readonly ILogger<CertificateController> _logger;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="certificateCache">To store the certificate</param>
    /// <param name="loggerFactory">The logger factory for the audit controller</param>
    /// <param name="signatureService">The signature service used by the certificate controller</param>
    public CertificateController(ICertificateCache certificateCache, 
        ILoggerFactory loggerFactory, 
        ISignatureService signatureService)
    {
        _certificateCache = certificateCache;
        _signatureService = signatureService;
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
        }
        catch (CryptographicException e)
        {
            _logger.LogError("Error creating the certificate: {Message}", e.Message);
            return BadRequest("Error creating the certificate.");
        }
        
        // Check that has a valid date
        if (certificate.NotAfter < DateTime.UtcNow)
        {
            _logger.LogError(Constants.CertificateHasExpired);
            return BadRequest(Constants.CertificateHasExpired);
        }
        if (DateTime.UtcNow < certificate.NotBefore)
        {
            _logger.LogError(Constants.CertificateNotYetValid);
            return BadRequest(Constants.CertificateNotYetValid);
        }

        // Try to add the certificate to the cache
        try
        {
            _certificateCache.AddCertificate(certificate);
        }
        catch (InvalidOperationException)
        {
            _logger.LogError(Constants.CertificateIsInvalid);
            return BadRequest(Constants.CertificateIsInvalid);
        }
        catch (Exception e)
        {
            _logger.LogError("Error adding the certificate to the cache {Message}", e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, "Error adding the certificate to the cache");
        }
        
        _logger.LogInformation(Constants.CertificateWasUploadedSuccessfully);
        return Ok(Constants.CertificateWasUploadedSuccessfully);
    }

    /// <summary>
    /// Retrieves the certificate for the key that Azure and the middleware use to sign the KEK (key encryption key) 
    /// </summary>
    /// <response code="200">If the certificate was retrieved successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    public async Task<IActionResult> GetAzureSigningCertificate()
    {
        try
        {
            var certificateAsPem = await _signatureService.GetKeyVaultCertificateAsX509PemString();
            _logger.LogInformation("Certificate was converted to PEM successfully");
            return Ok(certificateAsPem);
        }
        catch (CryptographicException)
        {
            _logger.LogError("Certificate was cryptographically invalid");
            return StatusCode(StatusCodes.Status500InternalServerError, "Certificate was cryptographically invalid");
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed in retrieving the certificate {Message}", e.Message);
            return StatusCode(e.Status, $"Azure failed in retrieving the certificate: {e.ErrorCode}");
        }
        catch (Exception e)
        {
            _logger.LogError("An error occured when retrieving the certificate from azure {Message}", e.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred when retrieving the certificate from azure");
        }
    }
}