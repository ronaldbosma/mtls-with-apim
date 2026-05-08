> [!WARNING]  
> This repository intentionally includes self-signed certificates (including private keys) **for local development and demo/template convenience only**.
> In real-world scenarios, **never** commit certificates with private keys to source control.
> Use proper secret/certificate management and generate certificates as part of your secure environment/tooling.
> We included these files here to keep this template easy to use without adding an extra dependency on certificate generation tools (for example, OpenSSL).

---

# Self Signed Certificates

The script [generate-client-certificates.ps1](./generate-client-certificates.ps1) can be used to generate the following self-signed certificate tree.

![self-signed certificates](../images/diagrams-self-signed-certificates.png)

See the [certificates](./certificates) folder for the generated certificates. The `.pfx` files are password protected with the password `P@ssw0rd`. They are valid until `May 8th, 2076`, except for the Expired certificate.
