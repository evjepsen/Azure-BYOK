using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("[controller]")]
public class KeyVaultController : Controller
{

    private readonly IKeyVaultService _keyVaultService;
    
    public KeyVaultController(IKeyVaultService keyVaultService)
    {
        _keyVaultService = keyVaultService;
    }
    
    [HttpGet("{kekName}")]
    public IActionResult CreateKeyEncryptionKey(string kekName)
    {
        var kek = _keyVaultService.GenerateKek(kekName);
        
        return Ok(kek.Value);
    }
    
    [HttpPost("{name}/{byokJson}")]
    public IActionResult ImportTdeKey(string name, string byokJson)
    {
        var kek = _keyVaultService.ImportKey(name, byokJson);
        
        return Ok(kek.Value);
    }
    
}