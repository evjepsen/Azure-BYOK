using System.Security.Cryptography;

namespace FakeHSM;

public class ExampleProgram
{
    
    public static void Main(string[] args)
    {
        Console.WriteLine("Starting HSM Simulation...");
        
        var filepath = args[1];
        // Read stdin pem from 
        // Check if the user provided a pem file
        if (args[0] != "--pem" || !filepath.EndsWith(".pem"))
        {
            Console.WriteLine("You must provide a .pem file as argument.");
            Console.WriteLine("Please use argument --pem <absolute path to pem file>");
            return;
        }

        // Check if the file is a pem file
        Console.WriteLine($"Loading PEM from: {args[0]}...");
        
        var pem = File.ReadAllText($"{filepath}");

        // Parse the PEM content
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        
        // Run your simulation
        var fakeHsm = new FakeHsm();
        var blobCiphertext = fakeHsm.GeneratePrivateKeyForBlob(rsa);
        Console.WriteLine("Generated private key...");
        
        
        var base64 = Convert.ToBase64String(blobCiphertext);
        // Console.WriteLine("Converting to base64...");
        
        Console.WriteLine($"Key ready to upload:\n{base64}");
        
        Console.WriteLine($"Press any key to exit...");
        Console.ReadKey();
    }
}