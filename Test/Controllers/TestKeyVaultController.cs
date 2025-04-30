using API;
using API.Controllers;
using API.Models;
using Azure;
using Azure.ResourceManager.Monitor;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Test.TestHelpers;

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
    private Mock<IKeyVaultManagementService> _mockKeyVaultManagementService;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<KeyVaultController>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(logger => logger.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
        
        _mockKeyVaultService = new Mock<IKeyVaultService>();
        _mockAlertService = new Mock<IAlertService>();
        _mockKeyVaultManagementService = new Mock<IKeyVaultManagementService>(); 
        var mockTokenService = new Mock<ITokenService>();
        _mockSignatureService = new Mock<ISignatureService>();
        
        _keyVaultController = new KeyVaultController(_mockKeyVaultService.Object, 
            _mockAlertService.Object,
            _mockKeyVaultManagementService.Object,
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
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Creating a new key encryption key: {kekName}");
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
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            "Azure failed to create new key encryption key");
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
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Error,
            "An unexpected error occurred while creating a new key encryption key");
    }

    [Test]
    public async Task ShouldValidImportEncryptedKeyRequestReturnOk()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
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
    public async Task ShouldRotateEncryptedKeyRequestReturnBadRequestWhenKeyDoesNotExists()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        
        _mockKeyVaultService.Setup(mock => mock.CheckIfKeyExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        
        // When I ask to import a key
        const string keyName = "testKey";
        var rotateRequest = new RotateEncryptedKeyRequest
        {
            Name = keyName,
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = ""
        };
        
        var result = await _keyVaultController.RotateKeyUsingNewEncryptedKey(rotateRequest);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Warning,
            $"The key {keyName} does not exist");
    }
    
    [Test]
    public async Task ShouldRotateEncryptedKeyRequestReturnBadRequestWhenKeyOperationsAreNotValid()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();

        const string errorMessage = "Error";
        var fakeResult = new KeyOperationsValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };

        _mockKeyVaultService.Setup(mock => mock.ValidateKeyOperations(It.IsAny<string[]>()))
            .Returns(fakeResult);
        
        // When I ask to import a key
        var rotateRequest = new RotateEncryptedKeyRequest
        {
            Name = "testKey",
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = ""
        };
        
        var result = await _keyVaultController.RotateKeyUsingNewEncryptedKey(rotateRequest);
        
        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Warning,
            errorMessage);
    }
    
    [Test]
    public async Task ShouldRequestWhereNoKeyVaultAlertReturnBadRequest()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // Override the standard configuration for error handling being tested
        _mockAlertService.Setup(mock => mock.CheckForKeyVaultAlertAsync()).ReturnsAsync(false);
        // When I ask to import a key
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Warning, 
            Constants.MissingKeyVaultAlert);
    }
    
    [Test]
    public async Task ShouldRequestWithNoActionGroupsReturnBadRequest()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = CreateImportEncryptedKeyRequest();
        uploadRequest.ActionGroups = [];
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Warning, 
            Constants.MissingActionGroup);
    }
    
    
    [Test]
    public async Task ShouldTooOldReplayOfMessageBeRejected()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = CreateImportEncryptedKeyRequest();
        uploadRequest.TimeStamp = DateTime.UtcNow.AddMinutes(-11);

        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Warning, 
            Constants.TheRequestIsNoLongerValid);
    }

    [Test]
    public async Task ShouldTooNewReplayOfMessageBeRejected()
    {
        // Given a key vault controller
        SetupMocksForSuccessfulRequest();
        // When I ask to import a key
        var uploadRequest = CreateImportEncryptedKeyRequest();
        uploadRequest.TimeStamp = DateTime.UtcNow.AddMinutes(11);
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Warning, 
            Constants.TheRequestIsNoLongerValid);
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
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Warning, 
            Constants.InvalidSignatureErrorMessage);
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
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));    
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger, 
            LogLevel.Error, 
            $"Azure failed to get the action group {actionGroupName}");
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
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Error,
            $"An unexpected error occurred while checking if the action group {actionGroupName} exists");
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
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Error,
            $"The action group {actionGroupName} does not exist");
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
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));    
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Error, 
            $"An unexpected error occurred when creating a key alert for the key {keyName}");
    
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Warning, 
            $"The key {keyName} was deleted because the alert could not be created");
        
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
        var uploadRequest = CreateImportEncryptedKeyRequest();
        
        var result = await _keyVaultController.ImportUserSpecifiedEncryptedKey(uploadRequest);
        // Then it should return a bad request result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));    
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger, 
            LogLevel.Error, 
            "There was an error while checking the signature:");
    }

    [Test]
    public async Task ShouldCorrectKeyDeleteReturnOk()
    {
        // Given a key vault controller
        var mockDeletedKey = new Mock<IDeletedKeyWrapper>();
        _mockKeyVaultService.Setup(mock => mock.DeleteKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(mockDeletedKey.Object);
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.DeleteKey(keyName);
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Deleting the key {keyName}");
    }

    [Test]
    public async Task ShouldFailedKeyDeleteReturnErrorMessageWhenInternalServerErrorOccurs()
    {
        // Given a key vault controller
        _mockKeyVaultService.Setup(mock => mock.DeleteKeyAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Something went wrong"));
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.DeleteKey(keyName);
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Deleting the key {keyName}");
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"An unexpected error occurred while deleting the key {keyName}");
    }
    
    [Test]
    public async Task ShouldFailedKeyDeleteReturnErrorMessageWhenAzureErrorOccurs()
    {
        // Given a key vault controller
        const int expectedStatusCode = StatusCodes.Status400BadRequest;
        _mockKeyVaultService.Setup(mock => mock.DeleteKeyAsync(It.IsAny<string>()))
            .ThrowsAsync(new RequestFailedException(expectedStatusCode, "Error"));
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.DeleteKey(keyName);
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(expectedStatusCode));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Deleting the key {keyName}");
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"Azure failed to delete the key {keyName}:");
    }
    
    [Test]
    public async Task ShouldSuccessfulRecoverReturnOk()
    {
        // Given a key vault controller
        var mockDeletedKey = new Mock<RecoverDeletedKeyOperation>();
        _mockKeyVaultService.Setup(mock => mock.RecoverDeletedKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(mockDeletedKey.Object);
        
        _mockKeyVaultManagementService.Setup(mock => mock.DoesKeyVaultHaveSoftDeleteEnabled())
            .Returns(true);
        
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.RecoverDeletedKey(keyName);
        
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Recovering the deleted key {keyName}");
    }
    
    [Test]
    public async Task ShouldReturnBadRequestWhenSoftDeleteIsNotEnabled()
    {
        // Given a key vault controller
        var mockDeletedKey = new Mock<RecoverDeletedKeyOperation>();
        _mockKeyVaultService.Setup(mock => mock.RecoverDeletedKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(mockDeletedKey.Object);
        
        _mockKeyVaultManagementService.Setup(mock => mock.DoesKeyVaultHaveSoftDeleteEnabled())
            .Returns(false);
        
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.RecoverDeletedKey(keyName);
        
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Warning,
            Constants.SoftDeleteErrorMessage);
    }
    
     [Test]
    public async Task ShouldSuccessfulPurgeReturnOk()
    {
        // Given a key vault controller
        var mockDeletedKey = new Mock<Response>();
        _mockKeyVaultService.Setup(mock => mock.PurgeDeletedKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(mockDeletedKey.Object);
        
        _mockKeyVaultManagementService.Setup(mock => mock.DoesKeyVaultHavePurgeProtection())
            .Returns(false);
        
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.PurgeDeletedKey(keyName);
        
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<StatusCodeResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Purging the deleted key {keyName}");
    }
    
    [Test]
    public async Task ShouldReturnBadRequestWhenPurgeProtectionIsEnabled()
    {
        // Given a key vault controller
        var mockDeletedKey = new Mock<RecoverDeletedKeyOperation>();
        _mockKeyVaultService.Setup(mock => mock.RecoverDeletedKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(mockDeletedKey.Object);
        
        _mockKeyVaultManagementService.Setup(mock => mock.DoesKeyVaultHavePurgeProtection())
            .Returns(true);
        
        // When I ask to delete a key
        const string keyName = "TestKey";
        var result = await _keyVaultController.PurgeDeletedKey(keyName);
        
        // Then it should be OK
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Warning,
            $"Cannot purge key {keyName} because purge protection is enabled on the vault");
    }

    
    [TearDown]
    public void TearDownController()
    {
        _keyVaultController.Dispose();
    }
    
    // Helper to set up mocks and create objects
    private static ImportEncryptedKeyRequest CreateImportEncryptedKeyRequest() 
    {
        return new ImportEncryptedKeyRequest
        {
            Name = "TestKey",
            ActionGroups = ["TestActionGroup"],
            KeyOperations = [],
            TimeStamp = DateTime.UtcNow,
            SignatureBase64 = "",
            KeyEncryptionKeyId = "",
            EncryptedKeyBase64 = "",

        };
    }
    
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