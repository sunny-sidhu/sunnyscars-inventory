using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Data;

public interface ICarStore
{
    Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Car?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Car> AddAsync(CarRequest request, CancellationToken cancellationToken = default);

    Task<Car?> UpdateAsync(string id, CarRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
