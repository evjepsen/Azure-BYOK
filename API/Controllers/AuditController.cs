using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("[controller]")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet("/keys/{numOfDays}")]
    public async Task<IActionResult> AuditKeyOperationsPerformed(int numOfDays)
    {
        var res = await _auditService.GetKeyOperationsPerformedAsync(numOfDays);
        return Ok(res);
    }
    
    [HttpGet("/vault/{numOfDays}")]
    public async Task<IActionResult> AuditVaultOperationsPerformed(int numOfDays)
    {
        var res = await _auditService.GetVaultOperationsPerformedAsync(numOfDays);
        return Ok(res);
    }
}