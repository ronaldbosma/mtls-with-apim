using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Configuration;

/// <summary>
/// Contains configuration settings for the integration tests.
/// </summary>
internal class TestConfiguration
{
    public required Uri AzureApiManagementGatewayUrl { get; init; }
    public required Uri AzureApplicationGatewayUrl { get; init; }
    public required Uri AzureKeyVaultUri { get; init; }

    public static TestConfiguration Load()
    {
        AzdDotEnv.Load(optional: true); // Loads Azure Developer CLI environment variables; optional since .env file might be missing in CI/CD pipelines

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        return new TestConfiguration
        {
            AzureApiManagementGatewayUrl = configuration.GetRequiredUri("AZURE_API_MANAGEMENT_GATEWAY_URL"),
            AzureApplicationGatewayUrl = new Uri($"http://{configuration.GetRequiredString("AZURE_APPLICATION_GATEWAY_PUBLIC_IP_ADDRESS")}"),
            AzureKeyVaultUri = configuration.GetRequiredUri("AZURE_KEY_VAULT_URI")
        };
    }
}