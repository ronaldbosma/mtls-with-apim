using System.Net;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

/// <summary>
/// Integration tests for the Unprotected API, which will forward requests to the Protected API using a backend that has a valid client certificate configured.
/// </summary>
[TestClass]
public sealed class UnprotectedApiTests
{
    private static readonly TestConfiguration Config = TestConfiguration.Load();

    [TestMethod]
    public async Task ValidateUsingPolicy_NoClientCertificateProvided_200OkReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync("unprotected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingContext_NoClientCertificateProvided_200OkReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync("unprotected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}