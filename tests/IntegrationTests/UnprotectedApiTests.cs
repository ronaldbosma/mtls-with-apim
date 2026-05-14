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

    /// <remarks>
    /// This test will fail for an APIM v2 tier where certificate chain validation is enabled, because the Unprotected API's client certificate will be untrusted.
    /// </remarks>
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

    /// <remarks>
    /// This test will fail for an APIM v2 tier where certificate chain validation is enabled, because the Unprotected API's client certificate will be untrusted.
    /// </remarks>
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