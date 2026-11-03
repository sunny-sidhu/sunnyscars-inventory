using Microsoft.AspNetCore.Http.HttpResults;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Endpoints;

public static class CarEndpoints
{
    public static IEndpointRouteBuilder MapCarEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1");
        var cars = v1.MapGroup("/cars").WithTags("Cars");

        cars.MapGet("/", GetAll).WithName("GetCars");
        cars.MapGet("/{id}", GetById).WithName("GetCarById");
        cars.MapPost("/", Create).WithName("CreateCar");
        cars.MapPut("/{id}", Update).WithName("UpdateCar");
        cars.MapDelete("/{id}", Delete).WithName("DeleteCar");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<Car>>> GetAll(ICarStore store, CancellationToken cancellationToken) =>
        TypedResults.Ok(await store.GetAllAsync(cancellationToken));

    private static async Task<Results<Ok<Car>, NotFound>> GetById(string id, ICarStore store, CancellationToken cancellationToken) =>
        await store.GetByIdAsync(id, cancellationToken) is { } car
            ? TypedResults.Ok(car)
            : TypedResults.NotFound();

    private static async Task<Created<Car>> Create(CarRequest request, ICarStore store, CancellationToken cancellationToken)
    {
        var car = await store.AddAsync(request, cancellationToken);
        return TypedResults.Created($"/v1/cars/{car.Id}", car);
    }

    private static async Task<Results<Ok<Car>, NotFound>> Update(
        string id, CarRequest request, ICarStore store, CancellationToken cancellationToken) =>
        await store.UpdateAsync(id, request, cancellationToken) is { } car
            ? TypedResults.Ok(car)
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound>> Delete(string id, ICarStore store, CancellationToken cancellationToken) =>
        await store.DeleteAsync(id, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}
