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
    private readonly IKeyVaultManagementService _keyVaultManagementService;
    private readonly ILogger<AuditController> _logger;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="auditService">The audit service used to access the logs</param>
    /// <param name="loggerFactory">The logger factory for the audit controller</param>
    /// <param name="keyVaultManagementService">The key vault management service used to access role assigmnets</param>
    public AuditController(IAuditService auditService, ILoggerFactory loggerFactory, IKeyVaultManagementService keyVaultManagementService)
    {
        _logger = loggerFactory.CreateLogger<AuditController>();
        _auditService = auditService;
        _keyVaultManagementService = keyVaultManagementService;
    }

    /// <summary>
    /// Get the key operations performed on all keys 
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <response code="200">The key activity log entries for the specified period in JSON format</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/keys/{numOfDays:int}")]
    public async Task<IActionResult> GetKeyOperationsPerformed(int numOfDays)
    {
        _logger.LogInformation("Getting key operations performed in the last {numOfDays} days", numOfDays);
        return await GetAuditLogs(_auditService.GetKeyOperationsPerformedAsync, numOfDays);
    }
    
    /// <summary>
    /// Get the operations performed on the vault
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <response code="200">Returns the logs of the operations performed on the vault in the time period</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/vault/{numOfDays:int}")]
    public async Task<IActionResult> GetVaultOperationsPerformed(int numOfDays)
    {
        _logger.LogInformation("Getting vault operations performed in the last {numOfDays} days", numOfDays);
        return await GetAuditLogs(_auditService.GetVaultOperationsPerformedAsync, numOfDays);
    }
    
    /// <summary>
    /// Get the activity logs for the key vault
    /// </summary>
    /// <param name="numOfDays">The time period to get logs in days</param>
    /// <response code="200">Returns the activity logs for the time period</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/activity/{numOfDays:int}")]
    public async Task<IActionResult> GetKeyVaultActivityLogs(int numOfDays)
    {
        _logger.LogInformation("Getting vault activity from the last {numOfDays} days", numOfDays);
        return await GetAuditLogs(_auditService.GetKeyVaultActivityLogsAsync, numOfDays);
    }
    
    /// <summary>
    /// Get the key vault's role assignments
    /// </summary>
    /// <response code="200">The role assignments linked to the key vault</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("/roleAssignments")]
    public async Task<IActionResult> GetRoleAssignments()
    {
        _logger.LogInformation("Getting role assignments");
        try
        {
            var res = await _keyVaultManagementService.GetRoleAssignmentsAsync();
            res = res.ToList();
            return Ok(res);
        }
        catch (Azure.RequestFailedException e)
        {
            _logger.LogError("Azure failed to get the role assignments: {errorMessage}", e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occured when getting the role assignments");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
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
            _logger.LogError("Azure failed to get the logs: {errorMessage}", e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Azure failed to get/find the logs: {errorMessage}", e.Message);
            return StatusCode(StatusCodes.Status404NotFound, e.Message);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occured when getting the logs");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
}