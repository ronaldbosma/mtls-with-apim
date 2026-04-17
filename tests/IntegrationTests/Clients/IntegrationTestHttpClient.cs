using System.Security.Cryptography.X509Certificates;

using IntegrationTests.Clients.Handlers;

namespace IntegrationTests.Clients;

/// <summary>
/// Represents an HTTP client used for integration testing, configured with a base address 
/// and a custom message handler for logging HTTP requests and responses.
/// Optionally, it can be configured to use a client certificate for authentication.
/// </summary>
internal class IntegrationTestHttpClient : HttpClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTestHttpClient"/> class with the specified base address.
    /// No authentication is configured for this client.
    /// </summary>
    /// <param name="baseAddress">The base address of the HTTP client.</param>
    public IntegrationTestHttpClient(Uri baseAddress)
        : base(new HttpMessageLoggingHandler(new HttpClientHandler()))
    {
        BaseAddress = baseAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTestHttpClient"/> class with the specified base address and client certificate.
    /// </summary>
    /// <param name="baseAddress">The base address of the HTTP client.</param>
    /// <param name="clientCertificate">The client certificate to use for authentication.</param>
    public IntegrationTestHttpClient(Uri baseAddress, X509Certificate2 clientCertificate)
        : base(CreateHandlerWithCertificate(clientCertificate))
    {
        BaseAddress = baseAddress;
    }

    private static HttpMessageLoggingHandler CreateHandlerWithCertificate(X509Certificate2 certificate)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(certificate);
        return new HttpMessageLoggingHandler(handler);
    }
}