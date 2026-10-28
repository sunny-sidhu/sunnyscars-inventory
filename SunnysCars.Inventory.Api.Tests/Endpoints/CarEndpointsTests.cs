using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Tests.Endpoints;

public sealed class CarEndpointsTests : IDisposable
{
    private static readonly Car[] SeedCars =
    [
        new(1, "Toyota", "Corolla", "Silver", 14995m, 2019, 42000, Transmission.Automatic),
        new(2, "Ford", "Focus", "Blue", 11250m, 2018, 56300, Transmission.Manual)
    ];

    private static readonly CarRequest NewCar = new("Mazda", "MX-5", "Red", 22500m, 2022, 12000, Transmission.Manual);

    private readonly string _dataDirectory = Directory.CreateTempSubdirectory("sunnyscars-api-tests-").FullName;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CarEndpointsTests()
    {
        var filePath = Path.Combine(_dataDirectory, "cars.json");
        File.WriteAllText(filePath, JsonSerializer.Serialize(SeedCars, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        // An absolute path overrides the content root, so the tests never touch the git-tracked Data/cars.json.
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("CarStore:FilePath", filePath));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        Directory.Delete(_dataDirectory, recursive: true);
    }

    [Fact]
    public async Task GetCars_ReturnsAllCars()
    {
        var response = await _client.GetAsync("/v1/cars");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SeedCars, await response.Content.ReadFromJsonAsync<Car[]>());
    }

    [Fact]
    public async Task GetCarById_ExistingId_ReturnsCar()
    {
        var response = await _client.GetAsync("/v1/cars/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SeedCars[1], await response.Content.ReadFromJsonAsync<Car>());
    }

    [Fact]
    public async Task CreateCar_ValidRequest_ReturnsCreatedCar()
    {
        var response = await _client.PostAsJsonAsync("/v1/cars", NewCar);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/v1/cars/3", response.Headers.Location?.OriginalString);
        Assert.Equal(NewCar.ToCar(3), await response.Content.ReadFromJsonAsync<Car>());
    }

    [Fact]
    public async Task UpdateCar_ExistingId_ReturnsUpdatedCar()
    {
        var response = await _client.PutAsJsonAsync("/v1/cars/1", NewCar);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(NewCar.ToCar(1), await response.Content.ReadFromJsonAsync<Car>());
    }

    [Fact]
    public async Task DeleteCar_ExistingId_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync("/v1/cars/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/v1/cars/1")).StatusCode);
    }
}
