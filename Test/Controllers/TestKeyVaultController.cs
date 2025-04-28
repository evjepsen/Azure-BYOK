using API;
using API.Controllers;
using API.Models;
using Azure;
using Azure.ResourceManager.Monitor;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Controllers;

[TestFixture]
[TestOf(typeof(KeyVaultController))]
public class TestKeyVaultController
{
    private KeyVaultController _keyVaultController;
    private Mock<ILogger<KeyVaultController>> _mockLogger;
    private Mock<IKeyVaultService> _mockKeyVaultService;
    private Mock<IAlertService> _mockAlertService;
    private Mock<ISignatureService> _mockSignatureService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<KeyVaultController>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(logger => logger.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
        
        _mockKeyVaultService = new Mock<IKeyVaultService>();
        _mockAlertService = new Mock<IAlertService>();
        var mockKeyVaultManagementService = new Mock<IKeyVaultManagementService>(); 
        var mockTokenService = new Mock<ITokenService>();
        _mockSignatureService = new Mock<ISignatureService>();
        
        _keyVaultController = new KeyVaultController(_mockKeyVaultService.Object, 
            _mockAlertService.Object,
            mockKeyVaultManagementService.Object,
            mockTokenService.Object,
            mockLoggerFactory.Object,
            _mockSignatureService.Object);
    }
    
    [Test]
    public async Task ShouldValidRequestKekReturnOk()
    {
        // Given a key vault controller
        const string kekName = "testKek";
        
        // When I ask to get the KEK
        var expectedResult = Mock.Of<KekSignedResponse>();
        _mockKeyVaultService.Setup(mock => mock.GenerateKekAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResult);
        
        var result = await _keyVaultController.RequestKeyEncryptionKey(kekName);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals($"Creating a new key encryption key: {kekName}", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShouldRequestKekFailWhenAzureErrorIsReturned()
    {
        // Given a key vault controller
        const string kekName = "testKek";
        const int statusCode = StatusCodes.Status404NotFound;
        const string errorCode = "ResourceNotFound";
        
        // When I request a KEK
        _mockKeyVaultService.Setup(mock => mock.GenerateKekAsync(It.IsAny<string>()))
            .ThrowsAsync(new RequestFailedException(statusCode, errorCode));

        var result = await _keyVaultController.RequestKeyEncryptionKey(kekName);

        // Then it should fail with a RequestFailedException
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Azure failed to create new key encryption key")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldRequestKekFailWhenUnknownErrorOccurs()
    {
        // Given a key vault controller
        const string kekName = "testKek";
        
        // When I request a KEK
        _mockKeyVaultService.Setup(mock => mock.GenerateKekAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var result = await _keyVaultController.RequestKeyEncryptionKey(kekName);

        // Then it should fail with a RequestFailedException
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("An unexpected error occurred while creating a new key encryption key", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShouldValidImportEncryptedKeyRequestReturnOk()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
    
    [Test]
    public async Task ShouldValidImportBlobRequestReturnOk()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = new ImportKeyBlobRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyTransferBlob = new KeyTransferBlob
            {
                Header = new HeaderObject
                {
                    Kid = "Kid",
                    Alg = "Alg",
                    Enc = "Enc"
                },
                Ciphertext = ""
            }
        };
        
        var result = await _keyVaultController.ImportUserSpecifiedTransferBlob(uploadRequest);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
    
    [Test]
    public async Task ShouldValidRotateBlobRequestReturnOk()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var rotateRequest = new RotateKeyBlobRequestBase
        {
            Name = "TestKey",
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyTransferBlob = new KeyTransferBlob
            {
                Header = new HeaderObject
                {
                    Kid = "Kid",
                    Alg = "Alg",
                    Enc = "Enc"
                },
                Ciphertext = ""
            }
        };
        
        var result = await _keyVaultController.RotateKeyUsingBlob(rotateRequest);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
    
    [Test]
    public async Task ShouldValidRotateEncryptedKeyRequestReturnOk()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var rotateRequest = new RotateEncryptedKeyRequest
        {
            Name = "TestKey",
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = ""
        };
        
        var result = await _keyVaultController.RotateKeyUsingNewEncryptedKey(rotateRequest);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
    
    [Test]
    public async Task ShouldRequestWhereNoKeyVaultAlertReturnBadRequest()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // Override the standard configuration for error handling being tested
        _mockAlertService.Setup(mock => mock.CheckForKeyVaultAlertAsync()).ReturnsAsync(false);
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(Constants.MissingKeyVaultAlert, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldRequestWithNoActionGroupsReturnBadRequest()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = [],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(Constants.MissingActionGroup, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    
    [Test]
    public async Task ShouldTooOldReplayOfMessageBeRejected()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow.AddDays(-11),
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(Constants.TheRequestIsNoLongerValid, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShouldTooNewReplayOfMessageBeRejected()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow.AddDays(11),
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(Constants.TheRequestIsNoLongerValid, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldInvalidSignatureBeRejected()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        _mockSignatureService.Setup(mock => mock.IsCustomerSignatureValid(
            It.IsAny<string>(),
            It.IsAny<byte[]>()
        )).Returns(false);        
        
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(Constants.InvalidSignatureErrorMessage, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldResultInBadRequestWhenAzureCannotFindTheActionGroup()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();

        const int statusCode = StatusCodes.Status404NotFound;
        const string errorCode = "ResourceNotFound";
        const string actionGroupName = "TestActionGroup";
        
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ThrowsAsync(new RequestFailedException(statusCode, errorCode));
        
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = [actionGroupName],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Azure failed to get the action group {actionGroupName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldResultInBadRequestWhenAnInternalErrorOccurs()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();

        const int statusCode = StatusCodes.Status500InternalServerError;
        const string actionGroupName = "TestActionGroup";
        
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Something went wrong"));
        
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = [actionGroupName],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals($"An unexpected error occurred while checking if the action group {actionGroupName} exists", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldResultInBadRequestWhenAnThereIsNoActionGroupData()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();

        const string actionGroupName = "TestActionGroup";
        
        var mockActionGroupResource = new Mock<ActionGroupResource>();
        mockActionGroupResource.Setup(mock => mock.HasData)
            .Returns(false);
        
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ReturnsAsync(mockActionGroupResource.Object);
        
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = [actionGroupName],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals($"The action group {actionGroupName} does not exist", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldDeleteKeyWhenFailsToCreateAlert()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();

        _mockAlertService.Setup(mock => mock.CreateAlertForKeyAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>()
        )).ThrowsAsync(new Exception("Something went wrong"));
        
        
        // When I ask to import a key
        const string keyName = "TestKey";
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = keyName,
            ActionGroups = ["actionGroupName"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals($"An unexpected error occurred when creating a key alert for the key {keyName}", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals($"The key {keyName} was deleted because the alert could not be created", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        _mockKeyVaultService.Verify(mock => mock.DeleteKeyAsync(keyName), Times.Once);
    }
    
    [Test]
    public async Task ShouldReturnBadRequestWhenErrorOccursWhenProcessingSignature()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        _mockSignatureService.Setup(mock => mock.IsCustomerSignatureValid(
            It.IsAny<string>(),
            It.IsAny<byte[]>()
        )).Throws(new Exception("Something went wrong"));        
        
        // When I ask to import a key
        var uploadRequest = new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));    
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("There was an error while checking the signature:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [TearDown]
    public void TearDownController()
    {
        _keyVaultController.Dispose();
    }
    
    // Helper to setup mocks
    private void SetupMocksForSuccessfulRequest()
    {
        // Mock of the alert service dependency
        _mockAlertService.Setup(mock => mock.CheckForKeyVaultAlertAsync())
            .ReturnsAsync(true);
        
        var mockActionGroupResource = new Mock<ActionGroupResource>();
        mockActionGroupResource.Setup(mock => mock.HasData).Returns(true);
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ReturnsAsync(mockActionGroupResource.Object);
        
        _mockAlertService.Setup(mock => mock.CreateAlertForKeyAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>()
        )).ReturnsAsync(Mock.Of<ScheduledQueryRuleResource>());
        
        // Mock of the key vault service dependency
        var fakeCustomJwk = new CustomJwk
        {
            Kid = "https://test-keyvault.vault.azure.net/keys/TestKey/1234567890"
        };
        
        var fakeKeyVaultUploadKeyResponse = new KeyVaultUploadKeyResponse
        {
            Key = fakeCustomJwk,
            Attributes = new KeyAttributes()
        };
        
        _mockKeyVaultService.Setup(mock => mock.UploadKey(
                It.IsAny<string>(), 
                It.IsAny<ITransferBlobStrategy>(),
                It.IsAny<string[]>()
                )).ReturnsAsync(fakeKeyVaultUploadKeyResponse);

        var fakeKeyOperationsValidationResult = new KeyOperationsValidationResult
        {
            IsValid = true,
            ErrorMessage = ""
        };
        _mockKeyVaultService.Setup(mock => mock.ValidateKeyOperations(It.IsAny<string[]>()))
            .Returns(fakeKeyOperationsValidationResult);

        _mockKeyVaultService.Setup(mock => mock.CheckIfKeyExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        // Mock of the signature service dependency
        _mockSignatureService.Setup(mock => mock.GetCustomerUploadSignedData(
            It.IsAny<byte[]>(), 
            It.IsAny<DateTime>()
            )).Returns([]);
        
        _mockSignatureService.Setup(mock => mock.IsCustomerSignatureValid(
            It.IsAny<string>(),
            It.IsAny<byte[]>()
            )).Returns(true);
    }
}