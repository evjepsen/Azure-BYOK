using Azure.Security.KeyVault.Keys;

namespace Infrastructure.Models;

public class KeyVaultUploadKeyResponse
{
    public required KeyInfo Key { get; set; }
    public required KeyAttributes Attributes { get; set; }
}

public class KeyInfo
{
    public string Kid { get; set; }
    public string Kty { get; set; }
    public List<string> Key_ops { get; set; }
    public string N { get; set; }
    public string E { get; set; }
}


public class KeyAttributes
{
    public bool Enabled { get; set; }
    public long Created { get; set; }
    public long Updated { get; set; }
    public string? RecoveryLevel { get; set; }
    public int RecoverableDays { get; set; }
    public bool Exportable { get; set; }
}