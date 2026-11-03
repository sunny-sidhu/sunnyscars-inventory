using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Models;
using SunnysCars.Inventory.Api.Tests.Fakes;

namespace SunnysCars.Inventory.Api.Tests.Endpoints;

public sealed class CarEndpointsTests : IDisposable
{
    private static readonly Car[] SeedCars =
    [
        new("6ae89b7ba121a7e475a6ae01", "Toyota", "Corolla", "Silver", 14995m, 2019, 42000, Transmission.Automatic),
        new("6ae89b7ba121a7e475a6ae02", "Ford", "Focus", "Blue", 11250m, 2018, 56300, Transmission.Manual)
    ];

    private static readonly CarRequest NewCar = new("Mazda", "MX-5", "Red", 22500m, 2022, 12000, Transmission.Manual);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CarEndpointsTests()
    {
        // Swapping in an in-memory store means the MongoDB client is never resolved, so no server is needed.
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<ICarStore>(new InMemoryCarStore(SeedCars))));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
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
        var response = await _client.GetAsync($"/v1/cars/{SeedCars[1].Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SeedCars[1], await response.Content.ReadFromJsonAsync<Car>());
    }

    [Fact]
    public async Task GetCarById_InvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/v1/cars/not-an-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCar_ValidRequest_ReturnsCreatedCar()
    {
        var response = await _client.PostAsJsonAsync("/v1/cars", NewCar);

        var created = await response.Content.ReadFromJsonAsync<Car>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal($"/v1/cars/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(NewCar.ToCar(created.Id), created);
    }

    [Fact]
    public async Task UpdateCar_ExistingId_ReturnsUpdatedCar()
    {
        var response = await _client.PutAsJsonAsync($"/v1/cars/{SeedCars[0].Id}", NewCar);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(NewCar.ToCar(SeedCars[0].Id), await response.Content.ReadFromJsonAsync<Car>());
    }

    [Fact]
    public async Task DeleteCar_ExistingId_ReturnsNoContent()
    {
        var response = await _client.DeleteAsync($"/v1/cars/{SeedCars[0].Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/v1/cars/{SeedCars[0].Id}")).StatusCode);
    }
}
