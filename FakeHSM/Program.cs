using System.Security.Cryptography;
using Infrastructure;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace FakeHSM;

public abstract class Program
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
        var kek = RSA.Create();
        kek.ImportFromPem(pem);
        Console.WriteLine("PEM loaded...");

        var tokenService = new TokenService(new NullLoggerFactory());
        var fakeHsm = new FakeHsm(tokenService);
        
        Console.WriteLine("Generate blob? (y/n)");
        var generateBlob = Console.ReadLine();

        if (generateBlob == "y")
        {
            Console.WriteLine("What is the id of the kek?");
            var kekId = Console.ReadLine();
            Console.WriteLine("Generating transfer blob...");
            if (kekId == null)
            {
                Console.WriteLine("You must provide a kek id.");
                return;
            }
            var blob = fakeHsm.GenerateBlobForUpload(kek, kekId);
            var jsonBlob = TokenHelper.SerializeJsonObject(blob);
            Console.WriteLine($"Blob ready to upload (JSON):\n {jsonBlob}");
        }
        else
        {
            Console.WriteLine("Encrypting customer key...");
            var base64 = fakeHsm.EncryptPrivateKeyForUpload(kek);
        
            Console.WriteLine($"Key ready to upload:\n{base64}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        
    }
}