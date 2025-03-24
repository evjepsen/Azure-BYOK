using API.Models;
using Azure.ResourceManager.Monitor;
using Azure;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller to interact with the Azure Key Vault
/// </summary>
[Route("[controller]")]
public class KeyVaultController : Controller
{
    private readonly IKeyVaultService _keyVaultService;
    private readonly IAlertService _alertService;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="keyVaultService">The key vault service used to interact with the Azure Key Vault</param>
    /// <param name="alertService">The alert service used to interact with the Azure Alert System</param>
    public KeyVaultController(IKeyVaultService keyVaultService, IAlertService alertService)
    {
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
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        
        return Ok(kek);
    }

    /// <summary>
    /// Endpoint used to import (into Azure) a user specified encrypted key
    /// </summary>
    /// <param name="request">The key import request</param>
    /// <response code="200">Returns the public part of the user specified key</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the key encryption key or action groups used don't exist</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    public async Task<IActionResult> ImportUserSpecifiedKey([FromBody] ImportKeyRequest request)
    {
        // There must be an alert for key vault usage setup
        var isThereAKeyVaultAlert = await _alertService.CheckForKeyVaultAlertAsync();

        if (!isThereAKeyVaultAlert)
        {
            return BadRequest("Missing a key vault alert");
        }
        
        // There must be at least one Action Group
        if (!request.ActionGroups.Any())
        {
            return BadRequest("Missing an Action Group");
        }
        
        // Each of them have to exist
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
            return BadRequest("Invalid base64 format for EncryptedKeyBase64");
        }
        
        // TODO: ADD CHECK THAT KEY ENCRYPTION KEY EXISTS
        
        // Upload the key to Azure
        var response = await _keyVaultService.UploadKey(request.Name, encryptedKey, request.KeyEncryptionKeyId);
        
        // Create an alert for the new key
        await _alertService.CreateAlertForKeyAsync($"{request.Name}-A", response.Key.Kid!, request.ActionGroups);
        
        return Ok(response);
    }

    /// <summary>
    /// Endpoint to get the public key part of a Key Encryption Key (KEK) in PEM format
    /// </summary>
    /// <param name="kekName"></param>
    /// <response code="200">Returns the public key of KEK in PEM format</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{kekName}")]
    public async Task<IActionResult> GetPublicKeyEncryptionKeyAsPem(string kekName)
    {
        try
        {
            var response = await _keyVaultService.DownloadPublicKekAsPemAsync(kekName);
            return Ok(response.PemString);
        }
        // Handle expected exception
        catch (RequestFailedException e)
        {
            return StatusCode(e.Status,e.ErrorCode);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Deletes a Key Encryption Key
    /// </summary>
    /// <param name="kekName"></param>
    /// <response code="200">Key was deleted</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("delete/{kekName}")]
    public async Task<IActionResult> DeleteKeyEncryptionKey(string kekName)
    {
        try
        {
            var response = await _keyVaultService.DeleteKekAsync(kekName); 
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
    /// Purge a deleted key
    /// </summary>
    /// <param name="kekName"></param>
    /// <response code="204">Deleted Key was purged</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("purgeDeletedKey/{kekName}")]
    public async Task<IActionResult> PurgeDeletedKeyEncryptionKey(string kekName)
    {
        try
        {
            var response = await _keyVaultService.PurgeDeletedKekAsync(kekName);
            return StatusCode(response.Status);
        }
        catch (OperationCanceledException e)
        {
            return StatusCode(StatusCodes.Status408RequestTimeout, "The operation was timed out.");
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