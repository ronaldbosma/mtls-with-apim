using IntegrationTests.Clients.Handlers;

namespace IntegrationTests.Clients
{
    /// <summary>
    /// Represents an HTTP client used for integration testing, configured with a base address 
    /// and a custom message handler for logging HTTP requests and responses.
    /// </summary>
    internal class IntegrationTestHttpClient : HttpClient
    {
        public IntegrationTestHttpClient(string baseAddress)
            : this(new Uri(baseAddress))
        {
        }

        public IntegrationTestHttpClient(Uri baseAddress)
            : base(new HttpMessageLoggingHandler(new HttpClientHandler()))
        {
            BaseAddress = baseAddress;
        }
    }
}