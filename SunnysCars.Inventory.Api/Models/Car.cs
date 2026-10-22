namespace SunnysCars.Inventory.Api.Models;

public sealed record Car(
    int Id,
    string Make,
    string Model,
    string Colour,
    decimal Price,
    int Year,
    int Mileage);
