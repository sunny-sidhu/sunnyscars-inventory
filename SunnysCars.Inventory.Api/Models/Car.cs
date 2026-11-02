namespace SunnysCars.Inventory.Api.Models;

public sealed record Car(
    string Id,
    string Make,
    string Model,
    string Colour,
    decimal Price,
    int Year,
    int Mileage,
    Transmission Transmission);
