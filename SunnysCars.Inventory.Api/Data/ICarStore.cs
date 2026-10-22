using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Data;

public interface ICarStore
{
    IReadOnlyList<Car> GetAll();

    Car? GetById(int id);

    Car Add(CarRequest request);

    Car? Update(int id, CarRequest request);

    bool Delete(int id);
}
