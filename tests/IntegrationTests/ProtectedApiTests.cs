using System.Net;
using System.Security.Cryptography.X509Certificates;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

[TestClass]
public sealed class ProtectedTests
{
    private static readonly X509Certificate2 ValidClientCertificate = X509CertificateLoader.LoadPkcs12FromFile(@"C:\repos\ronaldbosma\mtls-with-apim\self-signed-certificates\certificates\dev-client-01.pfx", "P@ssw0rd");
    private static readonly X509Certificate2 InvalidClientCertificate = X509CertificateLoader.LoadPkcs12FromFile(@"C:\repos\ronaldbosma\mtls-with-apim\self-signed-certificates\certificates\tst-client-01.pfx", "P@ssw0rd");

    [TestMethod]
    public async Task ValidateUsingPolicy_ValidClientCertificateProvided_200OkReturned()
    {
        // Arrange
        var config = TestConfiguration.Load();

        using var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl, ValidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingPolicy_NoClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        var config = TestConfiguration.Load();

        using var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingPolicy_InvalidClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        var config = TestConfiguration.Load();

        using var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl, InvalidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingContext_ValidClientCertificateProvided_200OkReturned()
    {
        // Arrange
        var config = TestConfiguration.Load();

        using var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl, ValidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingContext_NoClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        var config = TestConfiguration.Load();

        using var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingContext_InvalidClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        var config = TestConfiguration.Load();

        using var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl, InvalidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}