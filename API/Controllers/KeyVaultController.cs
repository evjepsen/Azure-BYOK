using API.Models;
using Azure;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller to interact with the Azure Key Vault
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ShouldBeAllowedEmail")]
[Route("[controller]")]
public class KeyVaultController : Controller
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly IAlertService _alertService;
    private readonly IKeyVaultManagementService _keyVaultManagementService;
    private readonly ILogger<KeyVaultController> _logger;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="keyVaultService">The key vault service used to interact with the Azure Key Vault</param>
    /// <param name="alertService">The alert service used to interact with the Azure Alert System</param>
    /// <param name="keyVaultManagementService">The key vault management service used to interact with key vault settings</param>
    /// <param name="loggerFactory">The key logger factory for the key vault controller</param>
    public KeyVaultController(IKeyVaultService keyVaultService, 
        IAlertService alertService, 
        IKeyVaultManagementService keyVaultManagementService,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<KeyVaultController>();
        _keyVaultManagementService = keyVaultManagementService;
        _keyVaultService = keyVaultService;
        _alertService = alertService;
    }
    
    /// <summary>
    /// Endpoint to create a new key encryption key (KEK) to be used to encrypt the user chosen key
    /// </summary>
    /// <param name="kekName">Name of the key encryption key</param>
    /// <returns>The public part of the key encryption key (To be used to encrypt the user chosen key)</returns>
    [HttpGet("create/{kekName}")]
    public async Task<IActionResult> CreateKeyEncryptionKey(string kekName)
    {
        try
        {
            _logger.LogInformation("Creating a new key encryption key");
            var kek = await _keyVaultService.GenerateKekAsync(kekName);
            return Ok(kek);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to create new key encryption key: {errorMessage}", e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while creating a new key encryption key");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Endpoint used to import (into Azure) a user specified encrypted key
    /// </summary>
    /// <param name="request">The key import request</param>
    /// <response code="200">Returns the public part of the user specified key</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">If the key encryption key or action groups used don't exist</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    public async Task<IActionResult> ImportUserSpecifiedKey([FromBody] ImportKeyRequest request)
    {
        // There must be an alert for key vault usage setup
        _logger.LogInformation("Checking key vault has key vault activity alert");
        var isThereAKeyVaultAlert = await _alertService.CheckForKeyVaultAlertAsync();

        if (!isThereAKeyVaultAlert)
        {
            _logger.LogWarning("Key vault is missing a key vault alert");
            return BadRequest("Missing a key vault alert");
        }
        
        // There must be at least one Action Group
        if (!request.ActionGroups.Any())
        {
            return BadRequest("Missing an Action Group");
        }
        
        // Each of them have to exist
        _logger.LogInformation("Checking action groups exist");
        foreach (var actionGroupName in request.ActionGroups)
        {
            try
            {
                var actionGroup = await _alertService.GetActionGroupAsync(actionGroupName);
                if (!actionGroup.HasData)
                {
                    return BadRequest($"The action group {actionGroupName} does not exist");
                }
            }
            catch (RequestFailedException e)
            {
                return StatusCode(e.Status, e.ErrorCode);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
            }
        }
        
        // Extract the encrypted key
        byte[] encryptedKey;
        try
        {
            encryptedKey = Convert.FromBase64String(request.EncryptedKeyBase64);
        }
        catch (FormatException)
        {
            _logger.LogError("The customer key was not in valid base64 format");
            return BadRequest("Invalid base64 format for EncryptedKeyBase64");
        }
        
        // Upload the key to Azure
        _logger.LogInformation("Uploading the key to Azure");
        KeyVaultUploadKeyResponse response;
        try
        { 
            response = await _keyVaultService.UploadKey(request.Name, encryptedKey, request.KeyEncryptionKeyId);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Azure failed to import key {keyName}: {ErrorMessage}", request.Name, e.Message);
            return BadRequest($"Azure failed to import key: {e.Message}");
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred when uploading the key {keyName}", request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
        
        // Create an alert for the new key
        _logger.LogInformation("Creating a log alert for the new key {keyName}", request.Name);
        try
        {
            await _alertService.CreateAlertForKeyAsync($"{request.Name}-A", response.Key.Kid!, request.ActionGroups);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to create an alert for the uploaded key {keyName}: {errorMessage}", request.Name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Azure failed to create an alert for the uploaded key {keyName}: {errorMessage}", request.Name, e.Message);
            return BadRequest($"Azure failed to create an alert for the uploaded key key: {e.Message}");
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred when creating a key alert for the key {keyName}", request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
        
        return Ok(response);
    }

    /// <summary>
    /// Endpoint to get the public key part of a Key Encryption Key (KEK) in PEM format
    /// </summary>
    /// <param name="kekName"></param>
    /// <response code="200">Returns the public key of KEK in PEM format</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{kekName}")]
    public async Task<IActionResult> GetPublicKeyEncryptionKeyAsPem(string kekName)
    {
        try
        {
            _logger.LogInformation("Accessing the public key of the key encryption key {kekName} in PEM format", kekName);
            var response = await _keyVaultService.DownloadPublicKekAsPemAsync(kekName);
            return Ok(response.PemString);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to get the key encryption key {kekName}: {errorMessage}", kekName, e.Message);
            return StatusCode(e.Status,e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while getting the key encryption key {kekName}", kekName);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Deletes a Key Encryption Key
    /// </summary>
    /// <param name="keyName"></param>
    /// <response code="200">Key was deleted</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("delete/{keyName}")]
    public async Task<IActionResult> DeleteKey(string keyName)
    {
        try
        {
            _logger.LogInformation("Deleting the key {kekName}", keyName);
            var response = await _keyVaultService.DeleteKeyAsync(keyName); 
            return Ok(response);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to delete the key {keyName}: {errorMessage}", keyName, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while deleting the key {keyName}", keyName);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
    
    /// <summary>
    /// Purge a deleted key
    /// </summary>
    /// <param name="keyName"></param>
    /// <response code="204">Deleted Key was purged</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("purgeDeletedKey/{keyName}")]
    public async Task<IActionResult> PurgeDeletedKey(string keyName)
    {
        try
        {
            var doesKeyVaultHavePurgeProtection = _keyVaultManagementService.DoesKeyVaultHavePurgeProtection();
            if (doesKeyVaultHavePurgeProtection)
            {
                _logger.LogWarning("Cannot delete key {keyName} because purge protection is enabled on the vault", keyName);
                return BadRequest("Purge protection is enabled on your vault");
            }
            
            // Try to complete the purge operation
            _logger.LogInformation("Purging the deleted key {keyName}", keyName);
            var response = await _keyVaultService.PurgeDeletedKeyAsync(keyName);
            return StatusCode(response.Status);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("The operation to purge the jet {keyName} timed out", keyName);
            return StatusCode(StatusCodes.Status408RequestTimeout, "The operation was timed out.");
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to purge the key {keyName}: {errorMessage}", keyName, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while purging the key {keyName}", keyName);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
    
    private async Task<IActionResult> CheckActionGroupExists(string actionGroupName)
    {
        IActionResult actionResult = Ok();
        try
        {
            var actionGroup = await _alertService.GetActionGroupAsync(actionGroupName);
            if (!actionGroup.HasData)
            {
                _logger.LogError("The action group {actionGroupName} does not exist", actionGroupName);
                actionResult = BadRequest($"The action group {actionGroupName} does not exist");
            }
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to get the action group {actionGroupName}: {errorMessage}", actionGroupName, e.Message);
            actionResult = StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while checking if the action group {actionGroupName} exists", actionGroupName);
            actionResult = StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }

        return actionResult;
    }
    /// <summary>
    /// Recover a deleted key encryption key
    /// </summary>
    /// <param name="kekName"></param>
    /// <response code="200">Deleted Key was recovered</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("recoverDeletedKey/{kekName}")]
    public async Task<IActionResult> RecoverDeletedKeyEncryptionKey(string kekName)
    {
        try
        {
            // Check if the key vault is enabled for soft delete
            var doesKeyVaultHaveSoftDelete = _keyVaultManagementService.DoesKeyVaultHaveSoftDeleteEnabled();
            if (!doesKeyVaultHaveSoftDelete)
            {
                return BadRequest("The key vault is not enabled for soft delete");
            }
            var response = await _keyVaultService.RecoverDeletedKekAsync(kekName);
            return Ok(response);
        }
        catch (RequestFailedException e)
        {
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
    
    /// <summary>
    /// Rotate a key encryption key
    /// </summary>
    /// <param name="kekName"></param>
    /// <response code="200">Key was rotated</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("rotate/{kekName}")]
    public async Task<IActionResult> RotateKeyEncryptionKey(string kekName)
    {
        try
        {
            var response = await _keyVaultService.RotateKekAsync(kekName);
            return Ok(response);
        }
        catch (RequestFailedException e)
        {
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
}