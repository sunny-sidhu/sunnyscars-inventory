using Microsoft.AspNetCore.Http.HttpResults;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Endpoints;

public static class CarEndpoints
{
    public static IEndpointRouteBuilder MapCarEndpoints(this IEndpointRouteBuilder app)
    {
        var cars = app.MapGroup("/cars").WithTags("Cars");

        cars.MapGet("/", GetAll).WithName("GetCars");
        cars.MapGet("/{id:int}", GetById).WithName("GetCarById");
        cars.MapPost("/", Create).WithName("CreateCar");
        cars.MapPut("/{id:int}", Update).WithName("UpdateCar");
        cars.MapDelete("/{id:int}", Delete).WithName("DeleteCar");

        return app;
    }

    private static Ok<IReadOnlyList<Car>> GetAll(ICarStore store) =>
        TypedResults.Ok(store.GetAll());

    private static Results<Ok<Car>, NotFound> GetById(int id, ICarStore store) =>
        store.GetById(id) is { } car
            ? TypedResults.Ok(car)
            : TypedResults.NotFound();

    private static Created<Car> Create(CarRequest request, ICarStore store)
    {
        var car = store.Add(request);
        return TypedResults.Created($"/cars/{car.Id}", car);
    }

    private static Results<Ok<Car>, NotFound> Update(int id, CarRequest request, ICarStore store) =>
        store.Update(id, request) is { } car
            ? TypedResults.Ok(car)
            : TypedResults.NotFound();

    private static Results<NoContent, NotFound> Delete(int id, ICarStore store) =>
        store.Delete(id)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}
