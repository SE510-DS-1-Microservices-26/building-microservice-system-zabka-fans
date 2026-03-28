using System.Net;
using System.Text.Json;
using CoreService.Api.Middleware;
using CoreService.Domain.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace InternshipTracker.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private async Task<HttpClient> CreateClientThatThrows(Exception exception)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(app =>
                {
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.Run(_ => throw exception);
                });
            })
            .StartAsync();

        return host.GetTestClient();
    }

    [Test]
    public async Task Middleware_DomainException_Underqualified_Returns400()
    {
        var client = await CreateClientThatThrows(
            new UnderqualifiedException("Level too low"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("code").GetString(), Is.EqualTo("Application.Underqualified"));
    }

    [Test]
    public async Task Middleware_DomainException_Duplicate_Returns409()
    {
        var client = await CreateClientThatThrows(
            new DuplicateApplicationException("Already applied"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("code").GetString(), Is.EqualTo("Application.Duplicate"));
    }

    [Test]
    public async Task Middleware_DomainException_CapacityExceeded_Returns409()
    {
        var client = await CreateClientThatThrows(
            new CapacityExceededException("No spots left"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Middleware_DomainException_AlreadyEnrolled_Returns409()
    {
        var client = await CreateClientThatThrows(
            new AlreadyEnrolledException("Already enrolled elsewhere"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Middleware_DomainException_InvalidState_Returns400()
    {
        var client = await CreateClientThatThrows(
            new InvalidApplicationStateException("Cannot reject enrolled"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Middleware_DomainException_ApplicationMismatch_Returns400()
    {
        var client = await CreateClientThatThrows(
            new ApplicationMismatchException("Wrong internship"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Middleware_DomainException_UserNotFound_Returns404()
    {
        var client = await CreateClientThatThrows(
            new UserNotFoundException(Guid.NewGuid()));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Middleware_UnhandledException_Returns500()
    {
        var client = await CreateClientThatThrows(
            new InvalidOperationException("Something broke"));

        var response = await client.GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("code").GetString(), Is.EqualTo("System.Failure"));
    }

    [Test]
    public async Task Middleware_ResponseBody_HasCorrectStructure()
    {
        var client = await CreateClientThatThrows(
            new DuplicateApplicationException("Already applied"));

        var response = await client.GetAsync("/test");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(body.RootElement.TryGetProperty("code", out _), Is.True);
        Assert.That(body.RootElement.TryGetProperty("description", out _), Is.True);
        Assert.That(body.RootElement.TryGetProperty("statusCode", out _), Is.True);
        Assert.That(body.RootElement.GetProperty("statusCode").GetInt32(), Is.EqualTo(409));
    }
}

