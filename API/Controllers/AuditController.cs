using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller to audit the operations performed on the Azure Vault instance and keys stored in it
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ShouldBeAllowedEmail")]
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
    /// <response code="200">The key activity log entries for the specified period in JSON format</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/keys/{numOfDays:int}")]
    public async Task<IActionResult> GetKeyOperationsPerformed(int numOfDays)
    {
        return await GetAuditLogs(_auditService.GetKeyOperationsPerformedAsync, numOfDays);
    }
    
    /// <summary>
    /// Get the operations performed on the vault
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <response code="200">Returns the logs of the operations performed on the vault in the time period</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/vault/{numOfDays:int}")]
    public async Task<IActionResult> GetVaultOperationsPerformed(int numOfDays)
    {
        return await GetAuditLogs(_auditService.GetVaultOperationsPerformedAsync, numOfDays);
    }
    
    /// <summary>
    /// Get the activity logs for the key vault
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <response code="200">Returns the activity logs for the time period</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/activity/{numOfDays:int}")]
    public async Task<IActionResult> GetKeyVaultActivityLogs(int numOfDays)
    {
        return await GetAuditLogs(_auditService.GetKeyVaultActivityLogsAsync, numOfDays);
    }

    // Helper method that handles error handling on calls to get audit logs
    private async Task<IActionResult> GetAuditLogs(Func<int, Task<string>> getAuditLogsMethod, int numOfDays)
    {
        try
        {
            var res = await getAuditLogsMethod(numOfDays);
            return Ok(res);
        }
        catch (Azure.RequestFailedException e)
        {
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (HttpRequestException e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
}