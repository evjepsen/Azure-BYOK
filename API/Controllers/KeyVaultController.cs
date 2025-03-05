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
    
    [HttpPost("{name}/{byokJson}/{kekId}")]
    public IActionResult ImportTdeKey(string name, byte[] byokJson, string kekId)
    {
        var kek = _keyVaultService.ImportKey(name, byokJson, kekId);
        
        return Ok(kek);
    }
    
}