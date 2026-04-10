using System.Net;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

[TestClass]
public sealed class ApimTests
{
    [TestMethod]
    public async Task Call_Status_Endpoint_On_API_Management()
    {
        // Arrange
        var config = TestConfiguration.Load();

        var apimClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync($"status-0123456789abcdef");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }
}