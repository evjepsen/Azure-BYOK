using System.Security.Cryptography;

namespace FakeHSM;

public class ExampleProgram
{
    
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting HSM Simulation...");

        // given a pem of a kek on the key vault
        var pem = """
                  -----BEGIN RSA PUBLIC KEY-----
                  MIIBCgKCAQEAraXkqjKzVBxcPRZ2MfrCFLkOZn75o/hp27ZeBb3ed72e5JyOdYgX
                  5QO/buupPGeMF4M2NyysZ/tVwQhLJdpbjiuYGAa2sCnYnVQnU2uaxbOVhxU0fw4T
                  657lsNk0taqWACjIBX7BB8zWHMIosLA9n6zQrLPfCmmub/RQXFtpX3OW3Dg3orXR
                  FaEpQAcC2i2yJXzglmz0b+g2GLeQMhQryweEo4B0COjnUSTzOsn0WLE6WbhbhAGy
                  X7bYxZbNxYub/u48UjE8wMLJwOONlax/D+3luS7+K7NxgeRD/BASBUZlm0+64P9j
                  2XA/3YEYNRHwZz/rHCyW14WpKUuKrdHj5wIDAQAB
                  -----END RSA PUBLIC KEY----- 
                  """;
            

        // Create a test KeyVaultKey
        // var pemFilePath = "../../../key.pem";
        // var pemContent = File.ReadAllText(pemFilePath);

        // Parse the PEM content
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        
        // Run your simulation
        var fakeHsm = new FakeHsm();
        var result = fakeHsm.SimulateHsm(rsa);
        
        // Display the result
        Console.WriteLine($"Simulation complete. Generated {result.Length} bytes of encrypted data.");
        Console.WriteLine("First 20 bytes (hex): " + BitConverter.ToString(result, 0, result.Length));
        var base64 = Convert.ToBase64String(result);
        // File.WriteAllText("../../../key.txt", base64);
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}