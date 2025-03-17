using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller to audit the operations performed on the Azure Vault instance and keys stored in it
/// </summary>
[Route("[controller]")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="auditService">The audit service used to access the logs</param>
    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get the key operations performed on all keys 
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <returns>The key activity log entries for the specified period in JSON format</returns>
    [HttpGet("/keys/{numOfDays}")]
    public async Task<IActionResult> GetKeyOperationsPerformed(int numOfDays)
    {
        var res = await _auditService.GetKeyOperationsPerformedAsync(numOfDays);
        return Ok(res);
    }
    
    /// <summary>
    /// Get the operations performed on the vault
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <returns>The vault activity log entries for the specified period in JSON format</returns>
    [HttpGet("/vault/{numOfDays}")]
    public async Task<IActionResult> GetVaultOperationsPerformed(int numOfDays)
    {
        var res = await _auditService.GetVaultOperationsPerformedAsync(numOfDays);
        return Ok(res);
    }
}