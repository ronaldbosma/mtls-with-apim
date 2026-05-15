using System.Net;
using System.Security.Cryptography.X509Certificates;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

[TestClass]
public class ApplicationGatewayTests
{
    private static readonly TestConfiguration Config = TestConfiguration.Load();
    private static X509Certificate2? s_validClientCertificate;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Check prerequisites
        if (!Config.IsApplicationGatewayIncluded)
        {
            Assert.Inconclusive("The Application Gateway is not deployed. Tests in this class will be skipped.");
        }

        // Load client certificates
        var keyVaultClient = new KeyVaultClient(Config.AzureKeyVaultUri);
        s_validClientCertificate = await keyVaultClient.GetCertificateAsync("dev-valid-client");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        s_validClientCertificate?.Dispose();
    }

    [TestMethod]
    public async Task CallApimStatusEndpointViaHttpsEndpoint_NoClientCertificate_200OkReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 443, Config.ApplicationGatewayHostname!);

        // Act
        var response = await agwClient.GetAsync("/status-0123456789abcdef");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CallApimStatusEndpointViaMtlsEndpoint_NoClientCertificate_400BadRequestReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!);

        // Act
        var response = await agwClient.GetAsync("/status-0123456789abcdef");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CallApimStatusEndpointViaMtlsEndpoint_ValidClientCertificate_200OkReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_validClientCertificate);
        // Act
        var response = await agwClient.GetAsync("/status-0123456789abcdef");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }



    /// <summary>
    /// Even though a valid client certificate is provided, a 401 Unauthorized response is expected because the Application Gateway terminates the TLS connection.
    /// The client certificate is never forwarded to the backend, so APIM has no way to validate it via the validate-client-certificate policy.
    /// </summary>
    [TestMethod]
    public async Task ValidateUsingPolicy_AgwMtlsEndpoint_ValidClientCertificate_401UnauthorizedReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_validClientCertificate);
        
        // Act
        var response = await agwClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Even though a valid client certificate is provided, a 401 Unauthorized response is expected because the Application Gateway terminates the TLS connection.
    /// The client certificate is never forwarded to the backend, so APIM has no way to validate it via the context.Request.Certificate property.
    /// </summary>
    [TestMethod]
    public async Task ValidateUsingContext_AgwMtlsEndpoint_ValidClientCertificate_401UnauthorizedReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_validClientCertificate);

        // Act
        var response = await agwClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}
