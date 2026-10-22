using System.ComponentModel.DataAnnotations;

namespace SunnysCars.Inventory.Api.Models;

/// <summary>
/// Payload used to create or replace a car. The Id is assigned by the store.
/// </summary>
public sealed record CarRequest(
    [property: Required, StringLength(50, MinimumLength = 1)] string Make,
    [property: Required, StringLength(50, MinimumLength = 1)] string Model,
    [property: Required, StringLength(30, MinimumLength = 1)] string Colour,
    [property: Range(typeof(decimal), "0", "10000000")] decimal Price,
    [property: Range(1886, 2100)] int Year,
    [property: Range(0, int.MaxValue)] int Mileage)
{
    public Car ToCar(int id) => new(id, Make, Model, Colour, Price, Year, Mileage);
}
