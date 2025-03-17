using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public class AuditService : IAuditService
{
    private readonly LogsQueryClient _client;
    private readonly IConfiguration _configuration; 

    public AuditService(IConfiguration configuration)
    {
        _configuration = configuration; 
        
        // Setup credentials for access
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions{});        
        
        // Create a LogQueryClient that can perform log queries
        _client = new LogsQueryClient(credential);
    }

    public async Task<string> GetKeyOperationsPerformedAsync(int numOfDays)
    {
        // Ensures that the result is given in JSON format
        const string query = "AzureDiagnostics | where OperationName startswith \"key\" | extend PackedRecord = pack_all()\n| project PackedRecord";
        return await GetLogs(numOfDays, query);
    }
    
    public async Task<string> GetVaultOperationsPerformedAsync(int numOfDays)
    {
        // Ensures that the result is given in JSON format
        const string query = "AzureDiagnostics | where OperationName startswith \"vault\" | extend PackedRecord = pack_all() | project PackedRecord"; 
        return await GetLogs(numOfDays, query);
    }

    private async Task<string> GetLogs(int numOfDays, string query)
    {
        var subscriptionId =  _configuration["SUBSCRIPTION_ID"] ?? throw new EnvironmentVariableNotSetException("The Subscription Id was not set");
        var resourceGroupName = _configuration["RESOURCE_GROUP_NAME"] ?? throw new EnvironmentVariableNotSetException("The Resource Group Name was not set");
        var resource = _configuration["KV_RESOURCE_NAME"] ?? throw new EnvironmentVariableNotSetException("The Resource Name was not set");
        
        // Setup the resourceId
        var resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{resource}";
        
        // Run the query to get all the key operations performed
        // Todo: Add proper error handling
        Response<LogsQueryResult> result = await _client.QueryResourceAsync(
            new ResourceIdentifier(resourceId), 
            query,            
            new QueryTimeRange(TimeSpan.FromDays(numOfDays))
        );

        if (result.Value.Status != 0)
        {
            return $"An error occured when fetching the logs - {result.Value.Error}";
        }
        
        var table = result.Value.Table;
        
        // Translate the parsed records into json and then serialise the whole thing as a Json List
        var rows = new List<object?>();
        foreach (var row in table.Rows)
        {
            var parsedRecord = JsonSerializer.Deserialize<object>(row.GetString(0));
            rows.Add(parsedRecord);
        }
        
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    }
}