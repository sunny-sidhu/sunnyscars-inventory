using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SunnysCars.Inventory.Api.Health;

namespace SunnysCars.Inventory.Api.Tests.Endpoints;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task GetHealth_WhenMongoIsHealthy_Returns200WithStatusCommitAndChecks()
    {
        using var factory = CreateFactory(HealthCheckResult.Healthy());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Healthy", body!.Status);
        Assert.Equal(HealthEndpoints.Commit, body.Commit);
        Assert.Equal(new Dictionary<string, string> { ["mongo"] = "Healthy" }, body.Checks);
    }

    [Fact]
    public async Task GetHealth_WhenMongoIsUnhealthy_Returns503WithoutFailureDetails()
    {
        using var factory = CreateFactory(HealthCheckResult.Unhealthy("MongoDB did not answer a ping.", new TimeoutException("secret detail")));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret detail", content);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("Unhealthy", body!.Status);
        Assert.Equal("Unhealthy", body.Checks["mongo"]);
    }

    [Fact]
    public void Commit_IsTheGitCommitTheTestedAppWasBuiltFrom()
    {
        // The SDK stamps SourceRevisionId from git, so a build from a clone always has a 40-character commit.
        Assert.Matches("^[0-9a-f]{40}$", HealthEndpoints.Commit);
    }

    // Replaces the MongoDB check with a fixed result, so no server is needed.
    private static WebApplicationFactory<Program> CreateFactory(HealthCheckResult mongoResult) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
            services.AddHealthChecks().AddCheck("mongo", () => mongoResult);
        }));

    private sealed record HealthResponse(string Status, string? Commit, Dictionary<string, string> Checks);
}
