using System.Text.Encodings.Web;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class AuditService : IAuditService
{
    private readonly LogsQueryClient _client;
    private readonly ApplicationOptions _applicationOptions;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IOptions<ApplicationOptions> applicationOptions, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        // Create the logger
        _logger = loggerFactory.CreateLogger<AuditService>();
        
        // Get the application options
        _applicationOptions = applicationOptions.Value;
        
        // Setup credentials for access
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());        
        
        // Create a LogQueryClient that can perform log queries
        _client = new LogsQueryClient(credential, new LogsQueryClientOptions
        {
            Transport = new HttpClientTransport(httpClientFactory.CreateClient("WaitAndRetry"))
        });
    }

    public async Task<string> GetKeyOperationsPerformedAsync(int numOfDays)
    {
        // Ensures that the result is given in JSON format
        _logger.LogInformation("Querying key operations performed");
        const string query = """
                             AzureDiagnostics 
                             | sort by TimeGenerated desc 
                             | where OperationName startswith "key" 
                             | extend PackedRecord = pack_all() 
                             | project PackedRecord
                             """;
        return await GetLogs(numOfDays, query);
    }
    
    public async Task<string> GetVaultOperationsPerformedAsync(int numOfDays)
    {
        // Ensures that the result is given in JSON format
        _logger.LogInformation("Querying vault operations performed");
        const string query = """
                             AzureDiagnostics 
                             | sort by TimeGenerated desc 
                             | where OperationName startswith "vault" 
                             | extend PackedRecord = pack_all() 
                             | project PackedRecord
                             """;
        return await GetLogs(numOfDays, query);
    }

    public async Task<string> GetKeyVaultActivityLogsAsync(int numOfDays)
    {
        _logger.LogInformation("Querying key vault activity");
        const string query = """
                             AzureActivity 
                             | sort by TimeGenerated desc  
                             | project-away Authorization, Claims, Properties
                             | extend PackedRecord = pack_all() 
                             | project PackedRecord
                             """; 
        return await GetLogs(numOfDays, query);
    }

    private async Task<string> GetLogs(int numOfDays, string query)
    {
        var subscriptionId = _applicationOptions.SubscriptionId;
        var resourceGroupName = _applicationOptions.ResourceGroupName;
        var keyVaultResource = _applicationOptions.KeyVaultResourceName;
        
        // Setup the resourceId
        var resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultResource}";
        
        // Run the query to get all the key operations performed
        Response<LogsQueryResult> result = await _client.QueryResourceAsync(
            new ResourceIdentifier(resourceId), 
            query,            
            new QueryTimeRange(TimeSpan.FromDays(numOfDays))
        );

        if (!result.HasValue || result.Value.Status != 0)
        {
            _logger.LogWarning("Failed to get logs from Azure");
            throw new RequestFailedException("The query did not return any results");
        }
        
        // Translate the parsed records into json and then serialise the whole thing as a Json List
        var table = result.Value.Table;
        var rows = new List<object?>();
        foreach (var row in table.Rows)
        {
            var parsedRecord = JsonSerializer.Deserialize<object>(row.GetString(0));
            rows.Add(parsedRecord);
        }
        
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}