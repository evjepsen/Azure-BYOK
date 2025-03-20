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
    /// <returns>The action group</returns>
    [HttpGet("/group/{name}")]
    public async Task<IActionResult> GetActionGroup(string name)
    {
        var res = await _alertService.GetActionGroupAsync(name);
        return Ok(res); 
    }
    
    /// <summary>
    /// Creates a new action group
    /// </summary>
    /// <param name="name">Name of the new action group</param>
    /// <param name="receivers">Name and email of the people who are to receive the alert</param>
    /// <returns>The new action group</returns>
    [HttpPost("/group/{name}")]
    public async Task<IActionResult> CreateActionGroup(string name, [FromBody] List<EmailReceiver> receivers)
    {
        var res = await _alertService.CreateActionGroupAsync(name, receivers);
        return Ok(res);
    }

    /// <summary>
    /// Creates a new key vault alert
    /// </summary>
    /// <param name="name">Name of the alert group</param>
    /// <param name="actionGroups">The action groups that should be notified</param>
    /// <returns>The created action group</returns>
    [HttpPost("/alert/{name}")]
    public async Task<IActionResult> CreateKeyVaultAlert(string name, [FromBody] List<string> actionGroups)
    {
        if (actionGroups.Count == 0)
        {
            return BadRequest("Must specify a least one action group");
        }
        
        var alert = await _alertService.CreateAlertForKeyVaultAsync(name, actionGroups);
        return Ok(alert);
    }
    
}