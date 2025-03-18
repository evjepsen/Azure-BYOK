namespace Infrastructure.Models;

// A proper request body when importing an RSA key:
// {
//   "key": {
//     "kty": "RSA-HSM",
//     "key_ops": [
//       "decrypt",
//       "encrypt"
//     ],
//     "key_hsm": "<Base64 encoded BYOK_BLOB>"
//   },
//   "attributes": {
//     "enabled": true
//   }
// }

public class UploadKeyRequestBody
{
    public required CustomJwk Key { get; init; }
    public required CustomJwkAttributes Attributes { get; init; }
}

public class CustomJwkAttributes
{
    public required bool Enabled { get; init; }
}