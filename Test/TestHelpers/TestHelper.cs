using Microsoft.Extensions.Configuration;

namespace Test.TestHelpers;

public static class TestHelper
{
    public static IConfiguration CreateTestConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.azure.json", false, true);
        var res = builder.Build();
        return res;
    }
}