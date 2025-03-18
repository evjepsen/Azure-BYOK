using Azure.ResourceManager.Monitor.Models;
using Infrastructure;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Test.TestHelpers;

namespace Test;

public class TestAlertService
{
    private IAlertService _alertService;

    [SetUp]
    public void Setup()
    {
        TestHelper.LoadEnvVariables();
        _alertService = new AlertService();
    }

    [Test]
    public async Task ShouldBePossibleToCreateAnActionGroup()
    {
        // Given an alert service and a list of emails
        var emailReceiver = new EmailReceiver
        {
            Name = "John Doe",
            Email = "john.doe@apple.com"
        };
        var emailReceivers = new List<EmailReceiver> { emailReceiver };
        // When I ask to add an Action group
        var actionGroup = await _alertService.CreateActionGroupAsync("test", emailReceivers);
        
        // Then it should be added
        Assert.True(actionGroup.HasData);
        
        // And have the correct data entries 
        Assert.That(actionGroup.Data.Name, Is.EqualTo("test"));
        Assert.That(actionGroup.Data.EmailReceivers.Count, Is.EqualTo(1));
        
        // And include the correct user
        Assert.That(actionGroup.Data.EmailReceivers[0].Name, Is.EqualTo("John Doe"));
        Assert.That(actionGroup.Data.EmailReceivers[0].EmailAddress, Is.EqualTo("john.doe@apple.com"));
    }

    [Test]
    public async Task ShouldBePossibleToGetAnActionGroup()
    {
        // Given an alert service and an action group
        var emailReceiver = new EmailReceiver
        {
            Name = "John Doe",
            Email = "john.doe@apple.com"
        };
        var emailReceivers = new List<EmailReceiver> { emailReceiver };
        await _alertService.CreateActionGroupAsync("test", emailReceivers);
        
        // When I ask to get it
        var actionGroup = await _alertService.GetActionGroupAsync("test");
        // Then it should be there
        Assert.True(actionGroup.HasData);
        Assert.That(actionGroup.Data.Name, Is.EqualTo("test"));
        Assert.That(actionGroup.Data.EmailReceivers.Count, Is.EqualTo(1));
        
        // And include the correct user
        Assert.That(actionGroup.Data.EmailReceivers[0].Name, Is.EqualTo("John Doe"));
        Assert.That(actionGroup.Data.EmailReceivers[0].EmailAddress, Is.EqualTo("john.doe@apple.com"));
    }

    [Test]
    public async Task ShouldBePossibleToCreateAlertForKey()
    {
        // Given an alert service and an action group
        var emailReceiver = new EmailReceiver
        {
            Name = "John Doe",
            Email = "john.doe@apple.com"
        };
        var emailReceivers = new List<EmailReceiver>{emailReceiver};
        var actionGroup = await _alertService.CreateActionGroupAsync("test", emailReceivers);
        IEnumerable<string> actionGroups = [actionGroup.Data.Name];
        
        // When I ask to add an alert
        var alert = await _alertService.CreateAlertForKeyAsync("ByokAlert","kek", actionGroups);
        // Then it should be there
        Assert.True(alert.HasData);
        
        // And the alert should have the correct attributes
        Assert.That(alert.Data.Name, Is.EqualTo("ByokAlert"));
        Assert.That(alert.Data.WindowSize, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(alert.Data.EvaluationFrequency, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.True(alert.Data.IsEnabled);
        Assert.That(alert.Data.Scopes[0].Contains("Microsoft.KeyVault/vaults"), Is.True);
        Assert.That(alert.Data.Actions.ActionGroups[0].Contains($"providers/microsoft.insights/actionGroups/{actionGroup.Data.Name}"));

        // And the condition should be correctly configured        
        var condition = alert.Data.CriteriaAllOf[0];
        Assert.That(condition.Query, Is.EqualTo("AzureDiagnostics | where OperationName startswith \"key\" | where id_s has \"kek\""));
        Assert.That(condition.TimeAggregation, Is.EqualTo(ScheduledQueryRuleTimeAggregationType.Count));
        Assert.That(condition.Operator, Is.EqualTo(MonitorConditionOperator.GreaterThanOrEqual));
        Assert.That(condition.Threshold, Is.EqualTo(1));
        Assert.That(condition.FailingPeriods.NumberOfEvaluationPeriods, Is.EqualTo(1));
        Assert.That(condition.FailingPeriods.MinFailingPeriodsToAlert, Is.EqualTo(1));
        Assert.That(condition.ResourceIdColumn, Is.EqualTo("ResourceId"));
    }
}