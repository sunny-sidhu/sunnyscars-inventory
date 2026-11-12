using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SunnysCars.Inventory.Api.Data;

namespace SunnysCars.Inventory.Api.Tests.Data;

public sealed class MongoHealthCheckTests
{
    [MongoFact]
    public async Task CheckHealthAsync_WhenServerAnswers_IsHealthy()
    {
        using var client = new MongoClient(MongoFactAttribute.ConnectionString);

        var result = await CheckHealthAsync(client);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServerIsUnreachable_ReportsFailureStatus()
    {
        // Nothing listens on port 1, and a short server selection timeout keeps the test fast.
        using var client = new MongoClient("mongodb://localhost:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");

        var result = await CheckHealthAsync(client);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    private static Task<HealthCheckResult> CheckHealthAsync(IMongoClient client)
    {
        var check = new MongoHealthCheck(client, Options.Create(new MongoCarStoreOptions()));
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("mongo", check, HealthStatus.Unhealthy, tags: null)
        };
        return check.CheckHealthAsync(context);
    }
}
