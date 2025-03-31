using Azure;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller to manage alerts and Action Groups
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "ShouldBeAllowedEmail")]
[Route("[controller]")]
public class AlertController : Controller
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertController> _logger;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="alertService">The alert service used to handle alerts and action groups</param>
    /// <param name="loggerFactory">The logger factory for the alert controller</param>
    public AlertController(IAlertService alertService, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AlertController>();
        _alertService = alertService;
    }
    
    /// <summary>
    /// Gets an action group
    /// </summary>
    /// <param name="name">Name of the action group</param>
    /// <response code="200">Returns the action group</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">If the action group couldn't be found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("/group/{name}")]
    public async Task<IActionResult> GetActionGroup(string name)
    {
        try
        {
            _logger.LogInformation("Getting action group {name}", name);
            var res = await _alertService.GetActionGroupAsync(name);
            return Ok(res); 
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to get action group {name}: {e}", name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occured when getting action group {name}", name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
  
    }
    
    /// <summary>
    /// Creates a new action group
    /// </summary>
    /// <param name="name">Name of the new action group</param>
    /// <param name="receivers">Name and email of the people who are to receive the alert</param>
    /// <response code="200">Returns the new action group</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("/group/{name}")]
    public async Task<IActionResult> CreateActionGroup(string name, [FromBody] List<EmailReceiver> receivers)
    {
        try
        {
            _logger.LogInformation("Creating action group {name}", name);
            var res = await _alertService.CreateActionGroupAsync(name, receivers);
            return Ok(res);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to create action group {name}: {errorMessage}", name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occured when creating action group {name}", name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Creates a new key vault alert
    /// </summary>
    /// <param name="name">Name of the alert group</param>
    /// <param name="actionGroups">The action groups that should be notified</param>
    /// <response code="200">Returns the new key vault alert</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("/alert/{name}")]
    public async Task<IActionResult> CreateKeyVaultAlert(string name, [FromBody] List<string> actionGroups)
    {
        // Check that the user has specified at least one action group
        if (actionGroups.Count == 0)
        {
            _logger.LogError("No action groups specified for alert {name}", name);
            return BadRequest("Must specify a least one action group");
        }
        
        // try to create the new key vault alert
        try
        {
            _logger.LogInformation("Creating key vault activity alert {name}", name);
            var alert = await _alertService.CreateAlertForKeyVaultAsync(name, actionGroups);
            return Ok(alert);
        }
        catch (RequestFailedException e)
        {
            _logger.LogError("Azure failed to create key vault alert {name}: {errorMessage}", name, e.Message);
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            _logger.LogError("An unexpected error occured when creating key vault alert {name}", name);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
    
}