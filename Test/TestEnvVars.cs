using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Infrastructure;
using Infrastructure.Interfaces;
using Test.TestHelpers;

namespace Test;

public class TestEnvVars
{
    [SetUp]
    public void Setup()
    {
        DotNetEnv.Env.TraversePath().Load();

    }

    [Test]
    public void VaultURIShouldExist()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        Console.WriteLine(currentDirectory);
        var vault_uri = Environment.GetEnvironmentVariable("VAULT_URI");
        Assert.IsNotNull(vault_uri);
    } 
}