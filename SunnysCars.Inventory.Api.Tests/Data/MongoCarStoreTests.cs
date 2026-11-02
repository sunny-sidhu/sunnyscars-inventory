using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Tests.Data;

public sealed class MongoCarStoreTests : IDisposable
{
    private static readonly CarRequest Corolla = new("Toyota", "Corolla", "Silver", 14995.50m, 2019, 42000, Transmission.Automatic);
    private static readonly CarRequest Focus = new("Ford", "Focus", "Blue", 11250m, 2018, 56300, Transmission.Manual);

    // Each test gets its own throwaway database, dropped in Dispose.
    private readonly MongoCarStoreOptions _options = new() { DatabaseName = $"sunnys_cars_test_{Guid.NewGuid():N}" };
    private readonly MongoClient? _client =
        MongoFactAttribute.ConnectionString is { Length: > 0 } connectionString ? new MongoClient(connectionString) : null;

    public void Dispose()
    {
        if (_client is null)
        {
            return;
        }

        _client.DropDatabase(_options.DatabaseName);
        _client.Dispose();
    }

    [MongoFact]
    public async Task AddAsync_AssignsObjectId_AndCarCanBeReadBack()
    {
        var store = CreateStore();

        var added = await store.AddAsync(Corolla);

        Assert.True(ObjectId.TryParse(added.Id, out _));
        Assert.Equal(Corolla.ToCar(added.Id), added);
        Assert.Equal(added, await store.GetByIdAsync(added.Id));
    }

    [MongoFact]
    public async Task GetAllAsync_ReturnsEveryCar()
    {
        var store = CreateStore();
        var corolla = await store.AddAsync(Corolla);
        var focus = await store.AddAsync(Focus);

        Assert.Equal([corolla, focus], await store.GetAllAsync());
    }

    [MongoFact]
    public async Task UpdateAsync_ExistingId_ReplacesCar()
    {
        var store = CreateStore();
        var added = await store.AddAsync(Corolla);

        var updated = await store.UpdateAsync(added.Id, Focus);

        Assert.Equal(Focus.ToCar(added.Id), updated);
        Assert.Equal(updated, await store.GetByIdAsync(added.Id));
    }

    [MongoFact]
    public async Task DeleteAsync_ExistingId_RemovesCar()
    {
        var store = CreateStore();
        var added = await store.AddAsync(Corolla);

        Assert.True(await store.DeleteAsync(added.Id));
        Assert.Null(await store.GetByIdAsync(added.Id));
        Assert.Empty(await store.GetAllAsync());
    }

    [MongoFact]
    public async Task UnknownOrInvalidId_IsNotFound()
    {
        var store = CreateStore();
        await store.AddAsync(Corolla);
        var unknownId = ObjectId.GenerateNewId().ToString();

        foreach (var id in new[] { unknownId, "not-an-id" })
        {
            Assert.Null(await store.GetByIdAsync(id));
            Assert.Null(await store.UpdateAsync(id, Focus));
            Assert.False(await store.DeleteAsync(id));
        }

        Assert.Single(await store.GetAllAsync());
    }

    [MongoFact]
    public async Task AddAsync_StoresDocumentInApiShape()
    {
        var store = CreateStore();
        var added = await store.AddAsync(Corolla);

        var document = await Collection().Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();

        Assert.Equal(
            new BsonDocument
            {
                { "_id", ObjectId.Parse(added.Id) },
                { "make", "Toyota" },
                { "model", "Corolla" },
                { "colour", "Silver" },
                { "price", new Decimal128(14995.50m) },
                { "year", 2019 },
                { "mileage", 42000 },
                { "transmission", "automatic" }
            },
            document);
    }

    [MongoFact]
    public async Task GetByIdAsync_ReadsDoublePrices()
    {
        // Documents loaded by a plain mongoimport store prices as doubles.
        var id = ObjectId.GenerateNewId();
        await Collection().InsertOneAsync(new BsonDocument
        {
            { "_id", id },
            { "make", "Nissan" },
            { "model", "Micra" },
            { "colour", "Orange" },
            { "price", 7500.0 },
            { "year", 2017 },
            { "mileage", 52400 },
            { "transmission", "manual" }
        });

        var car = await CreateStore().GetByIdAsync(id.ToString());

        Assert.Equal(new Car(id.ToString(), "Nissan", "Micra", "Orange", 7500m, 2017, 52400, Transmission.Manual), car);
    }

    private MongoCarStore CreateStore() => new(_client!, Options.Create(_options));

    private IMongoCollection<BsonDocument> Collection() =>
        _client!.GetDatabase(_options.DatabaseName).GetCollection<BsonDocument>(_options.CollectionName);
}
