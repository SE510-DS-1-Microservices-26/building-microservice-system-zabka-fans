using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InternshipTracker.Tests;

public class HealthEndpointTests
{
    [Test]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Arrange
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/health");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}