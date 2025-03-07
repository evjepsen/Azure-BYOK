namespace Infrastructure.Models;

public class KeyTransferBlob
{
        //  A proper Key Tranfer Blob according to the specification:
        // https://learn.microsoft.com/en-us/azure/key-vault/keys/byok-specification#key-transfer-blob
        //{
        //   "schema_version": "1.0.0",
        //   "header":
        //   {
        //     "kid": "<key identifier of the KEK>",
        //     "alg": "dir",
        //     "enc": "CKM_RSA_AES_KEY_WRAP"
        //   },
        //   "ciphertext":"BASE64URL(<ciphertext contents>)",
        //   "generator": "BYOK tool name and version; source HSM name and firmware version"
        // }

        public required string SchemaVersion { get; set; }
        // Header objects
        public required HeaderObjects Header { get; set; }
        // Body objects
        public required string Ciphertext { get; set; }
        public required string Generator { get; set; }
}

public class HeaderObjects
{
    public required string Kid { get; set; }
    public required string Alg { get; set; }
    public required string Enc { get; set; }
}
