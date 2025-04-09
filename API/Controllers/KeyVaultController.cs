using API.Models;
using Azure;
using Infrastructure.Interfaces;
using Infrastructure.TransferBlobStrategies;
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
    private readonly ITokenService _tokenService;
    private readonly ILogger<KeyVaultController> _logger;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="keyVaultService">The key vault service used to interact with the Azure Key Vault</param>
    /// <param name="alertService">The alert service used to interact with the Azure Alert System</param>
    /// <param name="keyVaultManagementService">The key vault management service used to interact with key vault settings</param>
    /// <param name="tokenService">The token service used when importing keys</param>
    /// <param name="loggerFactory">The keylogger factory for the key vault controller</param>
    public KeyVaultController(IKeyVaultService keyVaultService, 
        IAlertService alertService, 
        IKeyVaultManagementService keyVaultManagementService, 
        ITokenService tokenService,
        ILoggerFactory loggerFactory)

    {
        _logger = loggerFactory.CreateLogger<KeyVaultController>();
        _keyVaultManagementService = keyVaultManagementService;
        _tokenService = tokenService;
        _keyVaultService = keyVaultService;
        _alertService = alertService;
    }
    
    
    /// <summary>
    /// Endpoint to create a new key encryption key (KEK) to be used to encrypt the user chosen key
    /// </summary>
    /// <param name="kekName">Name of the key encryption key</param>
    /// <returns>The public part of the key encryption key (To be used to encrypt the user chosen key)</returns>
    /// <response code="429">Too many requests</response>
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
    /// <response code="429">Too many requests</response>
    [HttpPost("/import/encryptedKey")]
    public async Task<IActionResult> ImportUserSpecifiedEncryptedKey([FromBody] ImportEncryptedKeyRequest request)
    {
        // Check that the request (ImportEncryptedKeyRequest) object is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("The upload request could not be completed - the request body is invalid");
            return BadRequest("The request body is invalid (Properly JSON formatting error)");
        }
        
        
        var actionResult = await CheckValidityOfImportRequestAsync(request);

        if (actionResult is not OkResult)
        {
            return actionResult;
        }
        
        // Try to upload the key to Azure
        _logger.LogInformation("Uploading the key to Azure");
        var transferBlobStrategy = new EncryptedKeyTransferBlobStrategy(request.KeyEncryptionKeyId, request.EncryptedKeyBase64, _tokenService);
        return await ExecuteUploadWithErrorHandling(transferBlobStrategy, request);
    }

    /// <summary>
    /// Endpoint used to import (into Azure) a user specified key transfer blob
    /// </summary>
    /// <param name="request">The Key Import request containing the transfer blob</param>
    /// <response code="200">Returns the public part of the user specified key</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">If the key encryption key or action groups used don't exist</response>
    /// <response code="429">Too many requests</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("/import/blob")]
    public async Task<IActionResult> ImportUserSpecifiedTransferBlob([FromBody] ImportKeyBlobRequest request)
    {
        // Check that the request (ImportKeyBlobRequest) object is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("The upload request could not be completed - the request body is invalid");
            return BadRequest("The request body is invalid (Properly JSON formatting error)");
        }
        
        var actionResult = await CheckValidityOfImportRequestAsync(request);

        if (actionResult is not OkResult)
        {
            return actionResult;
        }
        
        // Try to upload the key to Azure
        _logger.LogInformation("Uploading the key to Azure");
        var transferBlobStrategy = new SpecifiedTransferBlobStrategy(request.KeyTransferBlob);
        return await ExecuteUploadWithErrorHandling(transferBlobStrategy, request);
    }

    /// <summary>
    /// Deletes a Key Encryption Key
    /// </summary>
    /// <param name="keyName"></param>
    /// <response code="200">Key was deleted</response>
    /// <response code="404">Key not found</response>
    /// <response code="400">Bad request. See the error code for details</response>
    /// <response code="500">Internal server error</response>
    /// <response code="429">Too many requests</response>
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
    /// <param name="keyName">The name of the key to purge</param>
    /// <response code="204">Deleted Key was purged</response>
    /// <response code="404">Key not found</response>
    /// <response code="429">Too many requests</response>
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
    
    /// <summary>
    /// Recover a deleted key encryption key
    /// </summary>
    /// <param name="kekName">The name of the key to recover</param>
    /// <response code="200">Deleted Key was recovered</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
    /// <response code="429">Too many requests</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("recoverDeletedKey/{kekName}")]
    public async Task<IActionResult> RecoverDeletedKey(string kekName)
    {
        try
        {
            // Check if the key vault is enabled for soft delete
            var doesKeyVaultHaveSoftDelete = _keyVaultManagementService.DoesKeyVaultHaveSoftDeleteEnabled();
            if (!doesKeyVaultHaveSoftDelete)
            {
                return BadRequest("The key vault is not enabled for soft delete");
            }
            _logger.LogInformation("Recovering the deleted key {kekName}", kekName);
            var response = await _keyVaultService.RecoverDeletedKeyAsync(kekName);
            return Ok(response);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to recover the key {keyName}: {errorMessage}", kekName, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while recovering the key {keyName}", kekName);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Rotate a key encryption key using the new encrypted key
    /// </summary>
    /// <param name="request">The key rotate request</param>
    /// <response code="200">Key was rotated</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
    /// <response code="429">Too many requests</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("rotate/encryptedKey")]
    public async Task<IActionResult> RotateKeyUsingNewEncryptedKey([FromBody] RotateEncryptedKeyRequest request)
    {
        // Check that the request (RotateEncryptedKeyRequest) object is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("The upload request could not be completed - the request body is invalid");
            return BadRequest("The request body is invalid (Properly JSON formatting error)");
        }
        
        // Specify the strategy
        var strategy = new EncryptedKeyTransferBlobStrategy(request.KeyEncryptionKeyId, request.EncryptedKeyBase64, _tokenService);
        return await RotateKeyWithErrorHandling(request, strategy);
    }
    
    /// <summary>
    /// Rotate a key encryption key using the new encrypted key stored in a blob
    /// </summary>
    /// <param name="request">The key rotate request</param>
    /// <response code="200">Key was rotated</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
    /// <response code="429">Too many requests</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("rotate/blob")]
    public async Task<IActionResult> RotateKeyUsingBlob([FromBody] RotateKeyBlobRequest request)
    {
        // Check that the request (RotateEncryptedKeyRequest) object is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("The upload request could not be completed - the request body is invalid");
            return BadRequest("The request body is invalid (Properly JSON formatting error)");
        }
        
        // Specify the strategy
        var strategy = new SpecifiedTransferBlobStrategy(request.KeyTransferBlob);
        return await RotateKeyWithErrorHandling(request, strategy);
    }

    // Helper method to check that import request (both blob and encrypted) are valid
    private async Task<IActionResult> CheckValidityOfImportRequestAsync(ImportKeyRequest request)
    {
        // There must be an alert for key vault usage setup
        var isThereAKeyVaultAlert = await _alertService.CheckForKeyVaultAlertAsync();

        if (!isThereAKeyVaultAlert)
        {
            return BadRequest("Missing a key vault alert");
        }
        
        // There must be at least one Action Group
        if (request.ActionGroups.Any())
        {
            return BadRequest("Missing an Action Group");
        }
        
        // Each of them have to exist
        foreach (var actionGroupName in request.ActionGroups)
        {
            var actionResult = await CheckActionGroupExists(actionGroupName);
            
            if (actionResult is not OkResult)
            {
                return actionResult;
            }
        }
        
        var keyOperationsValidationResult = _keyVaultService.ValidateKeyOperations(request.KeyOperations);

        if (!keyOperationsValidationResult.IsValid)
        {
            return BadRequest(keyOperationsValidationResult.ErrorMessage);
        }

        return Ok();
    }
    
    // Helper method to check if the action group exists
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
    
    // Helper method to complete the upload
    private async Task<IActionResult> ExecuteUploadWithErrorHandling(ITransferBlobStrategy transferBlobStrategy, ImportKeyRequest request)
    {
        KeyVaultUploadKeyResponse response;
        try
        {
            response = await _keyVaultService.UploadKey(request.Name, transferBlobStrategy, request.KeyOperations);
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
        
        // Try to create an alert for the new key
        _logger.LogInformation("Creating a log alert for the new key {keyName}", request.Name);
        bool created;
        try
        {
            await _alertService.CreateAlertForKeyAsync($"{request.Name}-Key-Alert", response.Key.Kid!, request.ActionGroups);
            created = true;
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to create an alert for the uploaded key {keyName}: {errorMessage}",
                request.Name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Azure failed to create an alert for the uploaded key {keyName}: {errorMessage}",
                request.Name, e.Message);
            return BadRequest($"Azure failed to create an alert for the uploaded key key: {e.Message}");
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred when creating a key alert for the key {keyName}",
                request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
        // Delete the new key if creating the alert fails
        if (!created)
        {
            await _keyVaultService.DeleteKeyAsync(request.Name);
            _logger.LogWarning("The key {keyName} was deleted because the alert could not be created", request.Name);
        }
        
        return Ok(response);
    }
    
    // Helper method to complete the rotation
    private async Task<IActionResult> RotateKeyWithErrorHandling(RotateKeyRequest request, ITransferBlobStrategy strategy)
    {
        // Try to rotate the key
        try
        {
            _logger.LogInformation("Checking that the key operations are valid");
            var keyOperationsValidationResult = _keyVaultService.ValidateKeyOperations(request.KeyOperations);

            if (!keyOperationsValidationResult.IsValid)
            {
                return BadRequest(keyOperationsValidationResult.ErrorMessage);
            }
            
            // Check that the key exists
            var doesKeyExist = await _keyVaultService.CheckIfKeyExistsAsync(request.KeyName);
            
            if (!doesKeyExist)
            {
                _logger.LogWarning("The key {keyName} does not exist", request.KeyName);
                return BadRequest("The key does not exist. Must add it using the import endpoint");
            }
            
            _logger.LogInformation("Rotating the key {keyName}", request.KeyName);
            var response = await _keyVaultService.UploadKey(request.KeyName, strategy, request.KeyOperations);
            
            return Ok(response);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to rotate the key {keyName}: {errorMessage}", request.KeyName, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while rotating the key {keyName}", request.KeyName);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
}