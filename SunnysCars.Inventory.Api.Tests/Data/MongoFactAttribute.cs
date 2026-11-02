namespace SunnysCars.Inventory.Api.Tests.Data;

/// <summary>
/// A fact that needs a real MongoDB server. It runs only when the <c>SUNNYSCARS_TEST_MONGO</c> environment variable
/// holds a connection string, and is skipped otherwise, so <c>dotnet test</c> still passes without MongoDB.
/// </summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "SUNNYSCARS_TEST_MONGO";

    public MongoFactAttribute()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            Skip = $"Set {ConnectionStringVariable} to a MongoDB connection string to run this test.";
        }
    }

    public static string? ConnectionString => Environment.GetEnvironmentVariable(ConnectionStringVariable);
}
