using MongoDB.Driver;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Endpoints;
using SunnysCars.Inventory.Api.Health;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();

builder.Services.Configure<MongoCarStoreOptions>(builder.Configuration.GetSection(MongoCarStoreOptions.SectionName));
// One client for the whole app: it owns the connection pool.
builder.Services.AddSingleton<IMongoClient>(services =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString(MongoCarStoreOptions.ConnectionStringName)
        ?? throw new InvalidOperationException(
            $"Connection string '{MongoCarStoreOptions.ConnectionStringName}' is not configured. Set ConnectionStrings:{MongoCarStoreOptions.ConnectionStringName}.");
    return new MongoClient(connectionString);
});
builder.Services.AddSingleton<ICarStore, MongoCarStore>();
// The MongoDB driver waits up to 30s to find a server; a health probe should answer well before that.
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>("mongo", timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapCarEndpoints();
app.MapHealthEndpoints();

app.Run();
