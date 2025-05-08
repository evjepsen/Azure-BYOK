import json
import base64
from cryptography import x509
from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.exceptions import InvalidSignature

# Load the certificate from a file
with open("cert", "rb") as f:
    cert_data = f.read()

# Parse the certificate
cert = x509.load_pem_x509_certificate(cert_data, default_backend())

# Now you can access certificate properties
cert_algo = cert.public_key_algorithm_oid._name
subject = cert.subject
issuer = cert.issuer
not_valid_before = cert.not_valid_before_utc
not_valid_after = cert.not_valid_after_utc
serial_number = cert.serial_number



print(f"***** Certificate information *****")
print(f"{cert_algo=}")
print(f"{subject=}")
print(f"{issuer=}")
print(f"{not_valid_before=}")
print(f"{not_valid_after=}")
print(f"{serial_number=}")

# If the JSON is in a file
with open("response-from-M.json", 'r') as file:
    data = json.load(file)

kek = json.dumps(data['kek'], separators=(",", ":"), ensure_ascii=True)
pem = str(data['pemString'])
signatureBase64 = data['base64EncodedSignature']
signature = base64.b64decode(signatureBase64)
public_key = cert.public_key()

msg = bytes(kek + pem, 'utf-8')
print("")
print("***** Signed data: *****")
print(kek+pem)
print("")

try:
    public_key.verify(
        signature,
        msg,
        padding=padding.PKCS1v15(),
        algorithm=hashes.SHA256()
    )
    print("[*] The signature is valid.")
except InvalidSignature:
    print("[*] The signature is invalid.")