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
        var emailReceivers = new List<EmailReceiver>{emailReceiver};
        // When I ask to add an Action group
        var actionGroup = await _alertService.CreateActionGroup("test", emailReceivers);
        // Then it should be added
        Assert.True(actionGroup.HasData);
    }
}