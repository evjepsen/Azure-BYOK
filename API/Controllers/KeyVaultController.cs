using API.Models;
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
    [HttpGet("{kekName}")]
    public async Task<IActionResult> CreateKeyEncryptionKey(string kekName)
    {
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        
        return Ok(kek);
    }

    /// <summary>
    /// Endpoint used to import (into Azure) a user specified encrypted key
    /// </summary>
    /// <param name="request">The key import request</param>
    /// <returns>When successful the public part of the user specified key</returns>
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
            var actionGroup = await _alertService.GetActionGroupAsync(actionGroupName);
            if (!actionGroup.HasData)
            {
                return BadRequest($"The action group {actionGroupName} does not exist");
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
        
        // Upload the key to Azure
        var response = await _keyVaultService.UploadKey(request.Name, encryptedKey, request.KeyEncryptionKeyId);
        
        // Create an alert for the new key
        await _alertService.CreateAlertForKeyAsync($"{request.Name}-A", response.Key.Kid!, request.ActionGroups);
        
        return Ok(response);
    }
    
}