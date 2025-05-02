using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test.Infrastructure;

[TestFixture]
[TestOf(typeof(KeyVaultManagementService))]
public class TestKeyVaultManagementService
{
    private IKeyVaultManagementService _keyVaultManagementService;

    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        _keyVaultManagementService = new KeyVaultManagementService(applicationOptions, new NullLoggerFactory());
    }

    [Test]
    public void ShouldBePossibleToCheckIfPurgeProtectionIsEnabled()
    {
        // Given a key vault management service
        // When I ask if purge protection is enabled
        var hasPurgeProtection = _keyVaultManagementService.DoesKeyVaultHavePurgeProtection();

        // Then it should return true
        Assert.That(hasPurgeProtection, Is.False);
    }

    [Test]
    public void ShouldBePossibleToCheckIfSoftDeleteIsEnabled()
    {
        // Given a key vault management service
        // When I ask if soft delete is enabled
        var hasSoftDelete = _keyVaultManagementService.DoesKeyVaultHaveSoftDeleteEnabled();

        // Then it should return true
        Assert.That(hasSoftDelete, Is.True);
    }

    [Test]
    public async Task ShouldBePossibleToViewRoleAssignments()
    {
        // Given a key vault management service
        // When I ask for the role assignments
        var roleAssignments = await _keyVaultManagementService.GetRoleAssignmentsAsync();
        roleAssignments = roleAssignments.ToList();
        
        // Then it should return a collection of role assignments
        Assert.That(roleAssignments, Is.Not.Null);
        Assert.That(roleAssignments, Is.Not.Empty);
    }
    
}