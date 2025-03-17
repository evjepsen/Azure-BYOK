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
    
    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="keyVaultService">The key vault service used to interact with the Azure Key Vault</param>
    public KeyVaultController(IKeyVaultService keyVaultService)
    {
        _keyVaultService = keyVaultService;
    }
    
    /// <summary>
    /// Endpoint to create a new key encryption key (KEK) to be used to encrypt the user chosen key
    /// </summary>
    /// <param name="kekName">Name of the key encryption key</param>
    /// <returns>The public part of the key encryption key (To be used to encrypt the user chosen key)</returns>
    [HttpGet("{kekName}")]
    public IActionResult CreateKeyEncryptionKey(string kekName)
    {
        var kek = _keyVaultService.GenerateKek(kekName);
        
        return Ok(kek.Value);
    }
    
    /// <summary>
    /// Endpoint used to import (into Azure) a user specified encrypted key
    /// </summary>
    /// <param name="name">Name of the encrypted key</param>
    /// <param name="encryptedKey">The key to upload in encrypted format</param>
    /// <param name="kekId">The id of the key encryption key used to encrypt the user specified key</param>
    /// <returns>When succesful the public part of the user specified key</returns>
    [HttpPost("{name}/{encryptedKey}/{kekId}")]
    public IActionResult ImportUserSpecifiedKey(string name, byte[] encryptedKey, string kekId)
    {
        var kek = _keyVaultService.UploadKey(name, encryptedKey, kekId);
        
        return Ok(kek);
    }
    
}