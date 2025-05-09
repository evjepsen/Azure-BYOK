using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using FakeHSM.Interfaces;
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
        IFakeHsm fakeHsm = new FakeHsm(tokenService);
        
        Console.WriteLine("Generate blob? (y/n)");
        var generateBlob = Console.ReadLine();
        
        byte[] keyData;
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
            var jsonBlob = TokenHelper.SerializeObject(blob);
            Console.WriteLine($"Blob ready to upload (JSON):\n {jsonBlob}");
            
            keyData = Encoding.UTF8.GetBytes(jsonBlob);
        }
        else
        {
            Console.WriteLine("Encrypting customer key...");
            var encryptedKey = fakeHsm.EncryptPrivateKeyForUpload(kek);
        
            Console.WriteLine($"Key ready to upload: {encryptedKey}");
            keyData = Convert.FromBase64String(encryptedKey);
        }
        // Sign the key
        Console.WriteLine("Signing the key ");
        var timeStamp = DateTime.UtcNow;
        Console.WriteLine($"The timestamp is: {timeStamp:yyyy-MM-ddTHH:mm:ssK}");
        var signature = fakeHsm.SignData(keyData, timeStamp);
        Console.WriteLine($"Key signature: {signature}");
        
        // Generate the self-signed certificate and save it to a specified location
        Console.WriteLine("Generating self-signed certificate ...");
        var cert = fakeHsm.GetCertificateForPrivateKey();
        Console.WriteLine("Specify where the cert should be saved");
        var file = Console.ReadLine();
        Console.WriteLine($"Saving the certificate at '{file}/cert.cert'");
        File.WriteAllBytes($"{file}/cert.cert", cert.Export(X509ContentType.Cert));
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        
    }
}