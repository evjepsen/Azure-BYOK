using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller to manage alerts and Action Groups
/// </summary>
[Route("[controller]")]
public class AlertController : Controller
{
    private readonly IAlertService _alertService;

    /// <summary>
    /// The constructor for the controller
    /// </summary>
    /// <param name="alertService">The alert service used to handle alerts and action groups</param>
    public AlertController(IAlertService alertService)
    {
        _alertService = alertService;
    }
    
    /// <summary>
    /// Gets an action group
    /// </summary>
    /// <param name="name">Name of the action group</param>
    /// <response code="200">Returns the action group</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the action group couldn't be found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("/group/{name}")]
    public async Task<IActionResult> GetActionGroup(string name)
    {
        try
        {
            var res = await _alertService.GetActionGroupAsync(name);
            return Ok(res); 
        }
        catch (Azure.RequestFailedException e)
        {
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
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
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("/group/{name}")]
    public async Task<IActionResult> CreateActionGroup(string name, [FromBody] List<EmailReceiver> receivers)
    {
        try
        {
            var res = await _alertService.CreateActionGroupAsync(name, receivers);
            return Ok(res);
        }
        catch (Azure.RequestFailedException e)
        {
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
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
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("/alert/{name}")]
    public async Task<IActionResult> CreateKeyVaultAlert(string name, [FromBody] List<string> actionGroups)
    {
        // Check that the user has specified at least one action group
        if (actionGroups.Count == 0)
        {
            return BadRequest("Must specify a least one action group");
        }
        
        // try to create the new key vault alert
        try
        {
            var alert = await _alertService.CreateAlertForKeyVaultAsync(name, actionGroups);
            return Ok(alert);
        }
        catch (Azure.RequestFailedException e)
        {
            return StatusCode(e.Status, e.ErrorCode);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }
    
}