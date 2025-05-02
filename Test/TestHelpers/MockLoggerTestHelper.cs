using Microsoft.Extensions.Logging;
using Moq;

namespace Test.TestHelpers;

public static class MockLoggerTestHelper
{
    public static void VerifyLogEntry<T>(Mock<ILogger<T>> logger, LogLevel logLevel, string message, Times times = default)
    {
        times = times == default ? Times.Once() : times;

        logger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals(message, o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
    
    public static void VerifyLogContains<T>(Mock<ILogger<T>> logger, LogLevel logLevel, string partialMessage)
    {
        logger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(partialMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}