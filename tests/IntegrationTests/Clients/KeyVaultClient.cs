using System.Security.Cryptography.X509Certificates;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace IntegrationTests.Clients;

/// <summary>
/// Provides a client for interacting with Azure Key Vault to retrieve secrets.
/// </summary>
internal class KeyVaultClient
{
    private readonly SecretClient _secretClient;

    /// <summary>
    /// Creates an instance of <see cref="KeyVaultClient"/> to interact with the specified Key Vault.
    /// </summary>
    /// <param name="keyVaultUri">The URI of the Azure Key Vault instance.</param>
    public KeyVaultClient(Uri keyVaultUri)
    {
        // Use a more specific credential in production scenarios. For best practices, see
        // https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/best-practices?tabs=aspdotnet
        _secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
    }

    /// <summary>
    /// Retrieves the value of a secret from Azure Key Vault asynchronously.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <returns>The value of the secret.</returns>
    public async Task<string> GetSecretValueAsync(string secretName)
    {
        var secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value.Value;
    }

    /// <summary>
    /// Retrieves a PKCS#12 certificate from Azure Key Vault and loads it as an <see cref="X509Certificate2"/>.
    /// </summary>
    /// <param name="secretName">The name of the secret containing the base64-encoded PFX.</param>
    /// <returns>The loaded certificate.</returns>
    public async Task<X509Certificate2> GetCertificateAsync(string secretName)
    {
        var base64Pfx = await GetSecretValueAsync(secretName);
        var pfxBytes = Convert.FromBase64String(base64Pfx);

        var keyStorageFlags = X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable;

        return X509CertificateLoader.LoadPkcs12(
            pfxBytes,
            string.Empty,
            keyStorageFlags);
    }
}