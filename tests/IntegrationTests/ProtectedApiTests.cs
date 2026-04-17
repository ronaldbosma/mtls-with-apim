using System.Net;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

[TestClass]
public sealed class ProtectedTests
{
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
}