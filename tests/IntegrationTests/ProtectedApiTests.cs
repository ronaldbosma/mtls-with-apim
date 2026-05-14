using System.Net;
using System.Security.Cryptography.X509Certificates;

using IntegrationTests.Clients;
using IntegrationTests.Configuration;

namespace IntegrationTests;

/// <summary>
/// Integration tests for the Protected API, which is configured to require client certificate authentication in APIM.
/// </summary>
[TestClass]
public sealed class ProtectedApiTests
{
    private static readonly TestConfiguration Config = TestConfiguration.Load();
    private static readonly X509Certificate2 ValidClientCertificate = X509CertificateLoader.LoadPkcs12FromFile($"{Config.DirectoryWithClientCertificates}/dev-valid-client.pfx", "P@ssw0rd");
    private static readonly X509Certificate2 UnregisteredClientCertificate = X509CertificateLoader.LoadPkcs12FromFile($"{Config.DirectoryWithClientCertificates}/dev-unregistered-client.pfx", "P@ssw0rd");
    private static readonly X509Certificate2 UntrustedClientCertificate = X509CertificateLoader.LoadPkcs12FromFile($"{Config.DirectoryWithClientCertificates}/tst-untrusted-client.pfx", "P@ssw0rd");
    private static readonly X509Certificate2 ExpiredClientCertificate = X509CertificateLoader.LoadPkcs12FromFile($"{Config.DirectoryWithClientCertificates}/dev-expired-client.pfx", "P@ssw0rd");
    private static readonly X509Certificate2 NotYetValidClientCertificate = X509CertificateLoader.LoadPkcs12FromFile($"{Config.DirectoryWithClientCertificates}/dev-notyetvalid-client.pfx", "P@ssw0rd");

    [ClassCleanup]
    public static void ClassCleanup()
    {
        ValidClientCertificate.Dispose();
        UnregisteredClientCertificate.Dispose();
        UntrustedClientCertificate.Dispose();
        ExpiredClientCertificate.Dispose();
        NotYetValidClientCertificate.Dispose();
    }

    /// <remarks>
    /// This test will fail for an APIM v2 tier where certificate chain validation is enabled, because the client certificate will be untrusted.
    /// </remarks>
    [TestMethod]
    public async Task ValidateUsingPolicy_ValidClientCertificateProvided_200OkReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, ValidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingPolicy_NoClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("ClientCertificateNotFound", response.Headers.GetValues("ErrorReason").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Client certificate missing", content);
    }

    /// <remarks>
    /// This test will fail for an APIM v2 tier where certificate chain validation is enabled, because the client certificate will be untrusted.
    /// </remarks>
    [TestMethod]
    public async Task ValidateUsingPolicy_UnregisteredClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, UnregisteredClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("ClientCertificateIdentityNotMatched", response.Headers.GetValues("ErrorReason").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid client certificate", content);
    }

    [TestMethod]
    public async Task ValidateUsingPolicy_UntrustedClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, UntrustedClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var expectedReason = Config.CertificateChainIsValidatedInProtectedApi ? "ClientCertificateNotTrusted" : "ClientCertificateIdentityNotMatched";
        Assert.AreEqual(expectedReason, response.Headers.GetValues("ErrorReason").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid client certificate", content);
    }

    [TestMethod]
    public async Task ValidateUsingPolicy_ExpiredClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, ExpiredClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("ClientCertificateExpired", response.Headers.GetValues("ErrorReason").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid client certificate", content);
    }

    [TestMethod]
    public async Task ValidateUsingPolicy_NotYetValidClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, NotYetValidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-policy");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("ClientCertificateNotYetValid", response.Headers.GetValues("ErrorReason").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid client certificate", content);
    }

    /// <remarks>
    /// This test will fail for an APIM v2 tier where certificate chain validation is enabled, because the client certificate will be untrusted.
    /// </remarks>
    [TestMethod]
    public async Task ValidateUsingContext_ValidClientCertificateProvided_200OkReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, ValidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidateUsingContext_NoClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsNotNull(response.ReasonPhrase);
        Assert.AreEqual("ClientCertificateNotFound", response.ReasonPhrase);
    }

    /// <remarks>
    /// This test will fail for an APIM v2 tier where certificate chain validation is enabled, because the client certificate will be untrusted.
    /// </remarks>
    [TestMethod]
    public async Task ValidateUsingContext_UnregisteredClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, UnregisteredClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsNotNull(response.ReasonPhrase);
        Assert.AreEqual("ClientCertificateIdentityNotMatched", response.ReasonPhrase);
    }

    [TestMethod]
    public async Task ValidateUsingContext_UntrustedClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, UntrustedClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsNotNull(response.ReasonPhrase);

        var expectedReason = Config.CertificateChainIsValidatedInProtectedApi ? "ClientCertificateNotTrusted" : "ClientCertificateIdentityNotMatched";
        Assert.AreEqual(expectedReason, response.ReasonPhrase);
    }

    [TestMethod]
    public async Task ValidateUsingContext_ExpiredClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, ExpiredClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsNotNull(response.ReasonPhrase);
        Assert.AreEqual("ClientCertificateExpired", response.ReasonPhrase);
    }

    [TestMethod]
    public async Task ValidateUsingContext_NotYetValidClientCertificateProvided_401UnauthorizedReturned()
    {
        // Arrange
        using var apimClient = new IntegrationTestHttpClient(Config.AzureApiManagementGatewayUrl, NotYetValidClientCertificate);

        // Act
        var response = await apimClient.GetAsync("protected/validate-using-context");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsNotNull(response.ReasonPhrase);
        Assert.AreEqual("ClientCertificateNotYetValid", response.ReasonPhrase);
    }
}