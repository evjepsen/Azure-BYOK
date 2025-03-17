namespace Infrastructure.Models;

//  A proper Key Tranfer Blob according to the specification:
//  {
//   "schema_version": "1.0.0",
//   "header":
//   {
//     "kid": "<key identifier of the KEK>",
//     "alg": "dir",
//     "enc": "CKM_RSA_AES_KEY_WRAP"
//   },
//   "ciphertext":"BASE64URL(<ciphertext contents>)",
//   "generator": "BYOK tool name and version; source HSM name and firmware version"
//  }
public class KeyTransferBlob
{
    public required string SchemaVersion { get; init; }
    public required HeaderObject Header { get; init; }
    public required string Ciphertext { get; init; }
    public required string Generator { get; init; }
}

public class HeaderObject
{
    public required string Kid { get; init; }
    public required string Alg { get; init; }
    public required string Enc { get; init; }
}
