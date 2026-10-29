# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Requires the .NET 10 SDK, pinned in `global.json` (10.0.402, rolling forward to newer patches only). Run from the repo root (`SunnysCars.Inventory.slnx` holds both projects).

```sh
dotnet build                                   # build API + tests
dotnet test                                    # run all xUnit tests
dotnet test --filter "FullyQualifiedName~JsonCarStoreTests"            # one test class
dotnet test --filter "FullyQualifiedName~CarRequestTests.Validate_ValidRequest_HasNoErrors"  # one test
dotnet run --project SunnysCars.Inventory.Api  # serves on http://localhost:5093
```

In Development, the OpenAPI document is at `/openapi/v1.json`. `SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.http` has sample requests for every endpoint.

CI (`.github/workflows/ci.yml`, GitHub Actions) runs `dotnet restore`, `dotnet build --no-restore` and `dotnet test --no-build` on every push and pull request to `main`, using the SDK from `global.json`. A failing test fails the run. It is annotated on the PR by the `GitHubActionsTestLogger` package, and the `.trx` results are uploaded as the `test-results` artifact.

## Architecture

This is an ASP.NET Core minimal API for a car inventory, with CRUD at `/v1/cars` (`Endpoints/CarEndpoints.cs`). Routes are versioned by nesting each resource group under a `/v1` group in `MapCarEndpoints`. There is no database: cars are stored in a JSON file.

- **Storage:** `JsonCarStore` is a singleton `ICarStore`. It loads the whole file into memory at startup, guards access with a `Lock`, and rewrites the entire file on every change (writes a `.tmp` file, then moves it into place). IDs are assigned as `max(id) + 1`.
- **Data file path:** `CarStore:FilePath` in `appsettings.json` (default `Data/cars.json`) is resolved against the content root. During `dotnet run` the content root is the project directory, so **POST/PUT/DELETE against a locally running API rewrite the git-tracked `SunnysCars.Inventory.Api/Data/cars.json`**. Use GETs or ids that don't exist when smoke-testing, or revert the file afterwards.
- **`Car` vs `CarRequest`:** `Car` (`Models/Car.cs`) is the stored and returned shape. `CarRequest` is the POST/PUT payload: it has no `Id`, carries the validation attributes, and maps to a `Car` via `ToCar(id)`. When you add a field to a car, update `Car`, `CarRequest` (including `ToCar`), every entry in `Data/cars.json`, the `.http` samples, and the test fixtures.
- **Validation:** DataAnnotations on `CarRequest` use the `[property: ...]` target so they apply to the generated properties. Failing requests are rejected with a 400 by .NET 10 minimal-API validation (`builder.Services.AddValidation()`) before the handler runs. A required enum field is declared nullable with `[Required, EnumDataType]`, so a missing value fails validation instead of defaulting to the first member (see `Transmission`).
- **JSON:** Serialization uses `JsonSerializerDefaults.Web` (camelCase). Enums serialize as lowercase strings through attributes on the enum itself (`[JsonConverter(typeof(JsonStringEnumConverter<T>))]` plus `[JsonStringEnumMemberName]`), not through global options. That way the file store and the HTTP layer agree without extra configuration.

## Tests

`SunnysCars.Inventory.Api.Tests` has unit tests plus one HTTP-level test per endpoint:
- **`CarEndpointsTests`:** runs the whole API in memory with `WebApplicationFactory<Program>`. It points `CarStore:FilePath` at an absolute path to a seeded temp file, so the tests never rewrite the git-tracked `Data/cars.json`.
- **`JsonCarStoreTests`:** builds a `JsonCarStore` directly over a temporary content root (using a stub `IHostEnvironment`). Persistence is checked by creating a second store over the same file.
- **`CarRequestTests`:** runs `Validator.TryValidateObject` on `CarRequest` and asserts which member names fail. Endpoint validation is not exercised.
