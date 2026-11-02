using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Data;

public sealed class MongoCarStoreOptions
{
    public const string SectionName = "CarStore";

    /// <summary>
    /// Name of the connection string (under <c>ConnectionStrings</c>) pointing at the MongoDB server.
    /// </summary>
    public const string ConnectionStringName = "Mongo";

    public string DatabaseName { get; set; } = "sunnys_cars";

    public string CollectionName { get; set; } = "inventory";
}

/// <summary>
/// Car store backed by a MongoDB collection. Each car is one document, keyed by an ObjectId <c>_id</c>
/// that the API exposes as a string.
/// </summary>
public sealed class MongoCarStore : ICarStore
{
    private readonly IMongoCollection<Car> _cars;

    static MongoCarStore()
    {
        // Store documents in the same shape as the API's JSON: camelCase fields and lowercase transmission names.
        ConventionRegistry.Register(
            "SunnysCars",
            new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
            type => type == typeof(Car));

        BsonClassMap.RegisterClassMap<Car>(map =>
        {
            map.AutoMap();
            map.MapCreator(c => new Car(c.Id, c.Make, c.Model, c.Colour, c.Price, c.Year, c.Mileage, c.Transmission));
            map.MapIdMember(c => c.Id).SetSerializer(new StringSerializer(BsonType.ObjectId));
            // Decimal128 keeps prices exact. Older documents stored as doubles still deserialize.
            map.MapMember(c => c.Price).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            map.MapMember(c => c.Transmission).SetSerializer(new LowercaseEnumSerializer<Transmission>());
        });
    }

    public MongoCarStore(IMongoClient client, IOptions<MongoCarStoreOptions> options)
    {
        _cars = client.GetDatabase(options.Value.DatabaseName).GetCollection<Car>(options.Value.CollectionName);
    }

    public async Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _cars.Find(FilterDefinition<Car>.Empty).ToListAsync(cancellationToken);

    public async Task<Car?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _cars.Find(ById(id)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Car> AddAsync(CarRequest request, CancellationToken cancellationToken = default)
    {
        var car = request.ToCar(ObjectId.GenerateNewId().ToString());
        await _cars.InsertOneAsync(car, cancellationToken: cancellationToken);
        return car;
    }

    public async Task<Car?> UpdateAsync(string id, CarRequest request, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _cars.FindOneAndReplaceAsync(
            ById(id),
            request.ToCar(id),
            new FindOneAndReplaceOptions<Car> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var result = await _cars.DeleteOneAsync(ById(id), cancellationToken);
        return result.DeletedCount > 0;
    }

    private static FilterDefinition<Car> ById(string id) => Builders<Car>.Filter.Eq(c => c.Id, id);

    /// <summary>
    /// Stores an enum as its lowercase member name, matching the names the enum's JSON converter writes.
    /// </summary>
    private sealed class LowercaseEnumSerializer<TEnum> : SerializerBase<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var value = context.Reader.ReadString();
            return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) && Enum.IsDefined(result)
                ? result
                : throw new FormatException($"'{value}' is not a valid {typeof(TEnum).Name}.");
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnum value) =>
            context.Writer.WriteString(value.ToString().ToLowerInvariant());
    }
}
