namespace Test.TestHelpers;

public static class TestHelper
{
    public static void LoadEnvVariables()
    {
        DotNetEnv.Env.TraversePath().Load();
    }
}