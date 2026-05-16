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
    private static X509Certificate2? s_unregisteredClientCertificate;
    private static X509Certificate2? s_untrustedClientCertificate;
    private static X509Certificate2? s_expiredClientCertificate;
    private static X509Certificate2? s_notYetValidClientCertificate;

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
        s_unregisteredClientCertificate = await keyVaultClient.GetCertificateAsync("dev-unregistered-client");
        s_untrustedClientCertificate = await keyVaultClient.GetCertificateAsync("tst-untrusted-client");
        s_expiredClientCertificate = await keyVaultClient.GetCertificateAsync("dev-expired-client", passwordSecretName: "client-certificate-password");
        s_notYetValidClientCertificate = await keyVaultClient.GetCertificateAsync("dev-notyetvalid-client");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        s_validClientCertificate?.Dispose();
        s_unregisteredClientCertificate?.Dispose();
        s_untrustedClientCertificate?.Dispose();
        s_expiredClientCertificate?.Dispose();
        s_notYetValidClientCertificate?.Dispose();
    }

    [TestMethod]
    public async Task ValidateFromAgw_AgwMtlsEndpoint_ValidClientCertificate_200OkReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_validClientCertificate);

        // Act
        var response = await agwClient.GetAsync("protected/validate-from-agw");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateFromAgw_AgwMtlsEndpoint_NoClientCertificate_400BadRequestReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!);

        // Act
        var response = await agwClient.GetAsync("protected/validate-from-agw");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No required SSL certificate was sent", content);
    }

    [TestMethod]
    public async Task ValidateFromAgw_AgwMtlsEndpoint_UnregisteredClientCertificate_401UnauthorizedReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_unregisteredClientCertificate);

        // Act
        var response = await agwClient.GetAsync("protected/validate-from-agw");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("ClientCertificateIdentityNotMatched", response.Headers.GetValues("ErrorReason").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid client certificate", content);
    }

    [TestMethod]
    public async Task ValidateFromAgw_AgwMtlsEndpoint_UntrustedClientCertificate_400BadRequestReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_untrustedClientCertificate);

        // Act
        var response = await agwClient.GetAsync("protected/validate-from-agw");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The SSL certificate error", content);
    }

    [TestMethod]
    public async Task ValidateFromAgw_AgwMtlsEndpoint_ExpiredClientCertificate_400BadRequestReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_expiredClientCertificate);

        // Act
        var response = await agwClient.GetAsync("protected/validate-from-agw");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The SSL certificate error", content);
    }

    [TestMethod]
    public async Task ValidateFromAgw_AgwMtlsEndpoint_NotYetValidClientCertificate_400BadRequestReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, 53029, Config.ApplicationGatewayHostname!, s_notYetValidClientCertificate);

        // Act
        var response = await agwClient.GetAsync("protected/validate-from-agw");

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The SSL certificate error", content);
    }


    /// <summary>
    /// Even though a valid client certificate is provided, a 401 Unauthorized response is expected because the Application Gateway terminates the TLS connection.
    /// The client certificate is not reused to create an mTLS connection between the Application Gateway and APIM,
    /// so APIM has no way to validate it via the validate-client-certificate policy.
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
    /// The client certificate is not reused to create an mTLS connection between the Application Gateway and APIM,
    /// so APIM has no way to validate it via the context.Request.Certificate property.
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
