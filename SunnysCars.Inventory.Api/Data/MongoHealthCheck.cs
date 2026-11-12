using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SunnysCars.Inventory.Api.Data;

/// <summary>
/// Reports whether the car store's MongoDB database answers a <c>ping</c>.
/// </summary>
public sealed class MongoHealthCheck(IMongoClient client, IOptions<MongoCarStoreOptions> options) : IHealthCheck
{
    private static readonly BsonDocument Ping = new("ping", 1);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetDatabase(options.Value.DatabaseName)
                .RunCommandAsync<BsonDocument>(Ping, cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "MongoDB did not answer a ping.", exception);
        }
    }
}
