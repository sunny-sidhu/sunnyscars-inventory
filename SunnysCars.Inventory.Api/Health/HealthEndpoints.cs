using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SunnysCars.Inventory.Api.Health;

public static class HealthEndpoints
{
    /// <summary>
    /// The git commit the app was built from, or null if the build had none. The SDK appends it to the
    /// informational version ("1.0.0+&lt;sha&gt;") from <c>SourceRevisionId</c>: taken from git in a local build,
    /// and passed in as the <c>GIT_COMMIT</c> build argument in the Docker build.
    /// </summary>
    public static string? Commit { get; } =
        typeof(HealthEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+', 2) is [_, var commit] ? commit : null;

    /// <summary>
    /// Maps <c>GET /health</c>. It is unversioned because it describes the running instance, not the API.
    /// Returns 200 when every check is Healthy or Degraded and 503 when any is Unhealthy, with a body like
    /// <c>{"status":"Healthy","commit":"&lt;sha&gt;","checks":{"mongo":"Healthy"}}</c>.
    /// Failure details stay in the logs, not the response.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteResponse });
        return app;
    }

    private static Task WriteResponse(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(
            new
            {
                Status = report.Status.ToString(),
                Commit,
                Checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString())
            },
            JsonSerializerOptions.Web);
}
