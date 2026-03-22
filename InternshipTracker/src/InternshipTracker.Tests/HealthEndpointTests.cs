using System.Net;
using InternshipTracker.Tests.Helpers;

namespace InternshipTracker.Tests;

public class HealthEndpointTests
{
    [Test]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}