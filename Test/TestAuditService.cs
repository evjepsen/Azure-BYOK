using Infrastructure;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Test.TestHelpers;

namespace Test;

public class TestAuditService
{
    private IAuditService _auditService;

    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        _auditService = new AuditService(applicationOptions, httpClientFactory, new NullLoggerFactory());
    }

    [Test]
    public async Task ShouldReturnLogsForKeyOperations()
    {
        // Given an Audit Service
        // When I ask for the key operations performed in the last 24 hours
        var result = await _auditService.GetKeyOperationsPerformedAsync(1);
        // Then it should return a result
        Assert.That(result, Is.Not.Empty);
    } 
    
    
    [Test]
    public async Task ShouldReturnLogsForVaultOperations()
    {
        // Given an Audit Service
        // When I ask for the vault operations performed in the last 24 hours
        var result = await _auditService.GetVaultOperationsPerformedAsync(1);
        // Then it should return a result
        Assert.That(result, Is.Not.Empty);
    } 
    
    [Test]
    public async Task ShouldReturnLogsOfKeyVaultActivity()
    {
        // Given an Audit Service
        // When I ask for the key vault operations performed in the last 24 hours
        var result = await _auditService.GetKeyVaultActivityLogsAsync(1);
        // Then it should return a result
        Assert.That(result, Is.Not.Empty);
    }
}