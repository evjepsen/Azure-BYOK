using System.Text;
using API.Models;
using Azure;
using Infrastructure.Helpers;
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
    private readonly ISignatureService _signatureService;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="keyVaultService">The key vault service used to interact with the Azure Key Vault</param>
    /// <param name="alertService">The alert service used to interact with the Azure Alert System</param>
    /// <param name="keyVaultManagementService">The key vault management service used to interact with key vault settings</param>
    /// <param name="tokenService">The token service used when importing keys</param>
    /// <param name="loggerFactory">The keylogger factory for the key vault controller</param>
    /// <param name="signatureService">The signature service used to check signature validity</param>
    public KeyVaultController(IKeyVaultService keyVaultService, 
        IAlertService alertService, 
        IKeyVaultManagementService keyVaultManagementService, 
        ITokenService tokenService,
        ILoggerFactory loggerFactory, 
        ISignatureService signatureService)

    {
        _logger = loggerFactory.CreateLogger<KeyVaultController>();
        _keyVaultManagementService = keyVaultManagementService;
        _tokenService = tokenService;
        _signatureService = signatureService;
        _keyVaultService = keyVaultService;
        _alertService = alertService;
        _signatureService = signatureService;
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
    [HttpPost("/import/encryptedKey")]
    public async Task<IActionResult> ImportUserSpecifiedEncryptedKey([FromBody] ImportEncryptedKeyRequest request)
    {
        // Check that the request (ImportEncryptedKeyRequest) object is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("The upload request could not be completed - the request body is invalid");
            return BadRequest("The request body is invalid (Properly JSON formatting error)");
        }

        // Check that the request is valid
        var keyData = Convert.FromBase64String(request.EncryptedKeyBase64);
        var actionResult = await CheckValidityOfImportRequestAsync(request, keyData);
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
        
        // Check that the request is valid
        var jsonKeyTransferBlob = TokenHelper.SerializeJsonObject(request.KeyTransferBlob);
        var keyData = Encoding.UTF8.GetBytes(jsonKeyTransferBlob);
        var actionResult = await CheckValidityOfImportRequestAsync(request, keyData);

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
    /// Endpoint to get the public key part of a Key Encryption Key (KEK) in PEM format
    /// </summary>
    /// <param name="kekName">The name of the key encryption key</param>
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
    /// <param name="keyName">The name of the key to purge</param>
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
    
    /// <summary>
    /// Recover a deleted key encryption key
    /// </summary>
    /// <param name="kekName">The name of the key to recover</param>
    /// <response code="200">Deleted Key was recovered</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
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
        var keyData = Convert.FromBase64String(request.EncryptedKeyBase64);
        var strategy = new EncryptedKeyTransferBlobStrategy(request.KeyEncryptionKeyId, request.EncryptedKeyBase64, _tokenService);
        return await RotateKeyWithErrorHandling(request, strategy, keyData);
    }
    
    /// <summary>
    /// Rotate a key encryption key using the new encrypted key stored in a blob
    /// </summary>
    /// <param name="requestBase">The key rotate request</param>
    /// <response code="200">Key was rotated</response>
    /// <response code="400">Bad request</response>
    /// <response code="404">Key not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("rotate/blob")]
    public async Task<IActionResult> RotateKeyUsingBlob([FromBody] RotateKeyBlobRequestBase requestBase)
    {
        // Check that the request (RotateEncryptedKeyRequest) object is valid
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("The upload request could not be completed - the request body is invalid");
            return BadRequest("The request body is invalid (Properly JSON formatting error)");
        }
        
        // Specify the strategy
        var jsonKeyTransferBlob = TokenHelper.SerializeJsonObject(requestBase.KeyTransferBlob);
        var keyData = Encoding.UTF8.GetBytes(jsonKeyTransferBlob);
        var strategy = new SpecifiedTransferBlobStrategy(requestBase.KeyTransferBlob);
        return await RotateKeyWithErrorHandling(requestBase, strategy, keyData);
    }

    // Helper method to check that import request (both blob and encrypted) are valid
    private async Task<IActionResult> CheckValidityOfImportRequestAsync(KeyRequestBase requestBase, byte[] keyData)
    {
        // There must be an alert for key vault usage setup
        var isThereAKeyVaultAlert = await _alertService.CheckForKeyVaultAlertAsync();

        if (!isThereAKeyVaultAlert)
        {
            return BadRequest("Missing a key vault alert");
        }

        if (requestBase is IActionGroupsRequest actionGroupRequest)
        {
            // There must be at least one Action Group
            if (!actionGroupRequest.ActionGroups.Any())
            {
                return BadRequest("Missing an Action Group");
            }
        
            // Each of them have to exist
            foreach (var actionGroupName in actionGroupRequest.ActionGroups)
            {
                var actionResult = await CheckActionGroupExists(actionGroupName);
            
                if (actionResult is not OkResult)
                {
                    return actionResult;
                }
            }
        }
        else
        {
            return BadRequest("Must be action groups present");
        }
        
        var keyOperationsValidationResult = _keyVaultService.ValidateKeyOperations(requestBase.KeyOperations);

        if (!keyOperationsValidationResult.IsValid)
        {
            return BadRequest(keyOperationsValidationResult.ErrorMessage);
        }
        
        return CheckSignature(requestBase, keyData);
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
    private async Task<IActionResult> ExecuteUploadWithErrorHandling<T>(ITransferBlobStrategy transferBlobStrategy, T requestBase) where T : KeyRequestBase, IActionGroupsRequest
    {
        KeyVaultUploadKeyResponse response;
        try
        {
            response = await _keyVaultService.UploadKey(requestBase.Name, transferBlobStrategy, requestBase.KeyOperations);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Azure failed to import key {keyName}: {ErrorMessage}", requestBase.Name, e.Message);
            return BadRequest($"Azure failed to import key: {e.Message}");
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred when uploading the key {keyName}", requestBase.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
        
        // Try to create an alert for the new key
        _logger.LogInformation("Creating a log alert for the new key {keyName}", requestBase.Name);
        bool created;
        try
        {
            await _alertService.CreateAlertForKeyAsync($"{requestBase.Name}-Key-Alert", response.Key.Kid!, requestBase.ActionGroups);
            created = true;
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to create an alert for the uploaded key {keyName}: {errorMessage}",
                requestBase.Name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Azure failed to create an alert for the uploaded key {keyName}: {errorMessage}",
                requestBase.Name, e.Message);
            return BadRequest($"Azure failed to create an alert for the uploaded key key: {e.Message}");
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred when creating a key alert for the key {keyName}",
                requestBase.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
        // Delete the new key if creating the alert fails
        if (!created)
        {
            await _keyVaultService.DeleteKeyAsync(requestBase.Name);
            _logger.LogWarning("The key {keyName} was deleted because the alert could not be created", requestBase.Name);
        }
        
        return Ok(response);
    }
    
    // Helper method to complete the rotation
    private async Task<IActionResult> RotateKeyWithErrorHandling(KeyRequestBase request, ITransferBlobStrategy strategy, byte[] keyData)
    {
        // Check that the signature is valid
        var isSignatureValidActionResult = CheckSignature(request, keyData);

        if (isSignatureValidActionResult is not OkResult)
        {
            return isSignatureValidActionResult;
        }
        
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
            var doesKeyExist = await _keyVaultService.CheckIfKeyExistsAsync(request.Name);
            
            if (!doesKeyExist)
            {
                _logger.LogWarning("The key {keyName} does not exist", request.Name);
                return BadRequest($"The key {request.Name} does not exist. Must add it using the import endpoint");
            }
            
            _logger.LogInformation("Rotating the key {keyName}", request.Name);
            var response = await _keyVaultService.UploadKey(request.Name, strategy, request.KeyOperations);
            
            return Ok(response);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to rotate the key {keyName}: {errorMessage}", request.Name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occurred while rotating the key {keyName}", request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
    
    // Helper method to check the signature
    private IActionResult CheckSignature(KeyRequestBase requestBase, byte[] keyData)
    {
        var data = _signatureService.GetSignedData(keyData, requestBase.TimeStamp);
        bool isSignatureValid;
        try
        {
            isSignatureValid = _signatureService.IsSignatureValid(requestBase.SignatureBase64, data);
        }
        catch (Exception e)
        {
            _logger.LogError("There was an error while checking the signature: {errorMessage}", e.Message);
            return BadRequest("There was an error while checking the signature");
        }

        if (!isSignatureValid)
        {
            _logger.LogWarning("The signature is invalid");
            return BadRequest("The signature is invalid");
        }
        
        if (requestBase.TimeStamp < DateTime.UtcNow.AddMinutes(-10) || DateTime.UtcNow.AddMinutes(10) < requestBase.TimeStamp)
        {
            _logger.LogWarning("The upload request is not longer valid");
            return BadRequest("The upload request is not longer valid");
        }

        return Ok();
    }
}