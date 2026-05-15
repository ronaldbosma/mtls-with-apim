using System.Net;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

[TestClass]
public class ApplicationGatewayTests
{
    private static readonly TestConfiguration Config = TestConfiguration.Load();

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Check prerequisites
        if (!Config.IsApplicationGatewayIncluded)
        {
            Assert.Inconclusive("The Application Gateway is not deployed. Tests in this class will be skipped.");
        }
    }

    [TestMethod]
    public async Task CallApimStatusEndpoint_NoClientCertificate_200OkReturned()
    {
        // Arrange
        using var agwClient = new IntegrationTestHttpClient(Config.ApplicationGatewayIpAddress!, Config.ApplicationGatewayHostname!);

        // Act
        var response = await agwClient.GetAsync("/status-0123456789abcdef");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
