namespace Infrastructure.Models;

public class KeyVaultUploadKeyResponse
{
    public required CustomJwk Key { get; set; }
    public required KeyAttributes Attributes { get; set; }
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