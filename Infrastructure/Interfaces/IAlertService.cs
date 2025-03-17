using Azure.ResourceManager.Monitor;
using Infrastructure.Models;

namespace Infrastructure.Interfaces;

public interface IAlertService
{
    public Task<string> CreateAlertForKeyAsync(string keyIdentifier);

    public Task<ActionGroupResource> CreateActionGroup(string name, IEnumerable<EmailReceiver> emails);
}