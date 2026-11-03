using MongoDB.Bson;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICarStore"/> for endpoint tests, so they run without a MongoDB server.
/// Ids are ObjectId strings, like the ones <see cref="MongoCarStore"/> assigns.
/// </summary>
public sealed class InMemoryCarStore(IEnumerable<Car> seed) : ICarStore
{
    private readonly Lock _lock = new();
    private readonly List<Car> _cars = seed.ToList();

    public Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Car>>(_cars.ToList());
        }
    }

    public Task<Car?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_cars.FirstOrDefault(c => c.Id == id));
        }
    }

    public Task<Car> AddAsync(CarRequest request, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var car = request.ToCar(ObjectId.GenerateNewId().ToString());
            _cars.Add(car);
            return Task.FromResult(car);
        }
    }

    public Task<Car?> UpdateAsync(string id, CarRequest request, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _cars.FindIndex(c => c.Id == id);
            if (index < 0)
            {
                return Task.FromResult<Car?>(null);
            }

            _cars[index] = request.ToCar(id);
            return Task.FromResult<Car?>(_cars[index]);
        }
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_cars.RemoveAll(c => c.Id == id) > 0);
        }
    }
}
