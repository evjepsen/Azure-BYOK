using System.Security.Cryptography;

namespace Test.TestHelpers;

public static class TestHelper
{
    // Helper method to generate a random key
    public static byte[] GenerateCiphertext()
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[32]; // Adjust size as needed
        rng.GetBytes(randomBytes);
        return randomBytes;
    }

    public static RSAParameters CreateTestKey()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportParameters(true);
    }
}