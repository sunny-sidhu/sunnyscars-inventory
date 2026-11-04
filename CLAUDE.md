# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Requires the .NET 10 SDK, pinned in `global.json` (10.0.402, rolling forward to newer patches only). Run from the repo root (`SunnysCars.Inventory.slnx` holds both projects).

```sh
dotnet build                                   # build API + tests
dotnet test                                    # run all xUnit tests
SUNNYSCARS_TEST_MONGO=mongodb://localhost:27017 dotnet test           # also run the MongoCarStore tests
dotnet test --filter "FullyQualifiedName~CarEndpointsTests"            # one test class
dotnet test --filter "FullyQualifiedName~CarRequestTests.Validate_ValidRequest_HasNoErrors"  # one test
dotnet run --project SunnysCars.Inventory.Api  # serves on http://localhost:5093 (needs MongoDB, see below)
mongosh "mongodb://localhost:27017" scripts/seed-mongodb.js  # seed/migrate sunnys_cars.inventory (SEED_DATABASE/SEED_COLLECTION override)
```

In Development, the OpenAPI document is at `/openapi/v1.json`. `SunnysCars.Inventory.Api/SunnysCars.Inventory.Api.http` has sample requests for every endpoint.

CI (`.github/workflows/ci.yml`, GitHub Actions) runs `dotnet restore`, `dotnet build --no-restore` and `dotnet test --no-build` on every push and pull request to `main`, using the SDK from `global.json`. It starts a `mongo:7` service container and sets `SUNNYSCARS_TEST_MONGO`, so the `MongoCarStore` tests run there. A failing test fails the run. It is annotated on the PR by the `GitHubActionsTestLogger` package, and the `.trx` results are uploaded as the `test-results` artifact. A separate `docker-build` job builds the container image for `linux/amd64` (it pushes nothing), so a broken Dockerfile fails the PR.

The same workflow (named `CI/CD`) has a `deploy` job (SCI-8). It runs only on a push to `main`, so after a PR merge, and only once `build-and-test` and `docker-build` have passed. It never runs for pull requests. It does these steps:
1. Logs in to Azure with OIDC (`azure/login`, no stored password) using the `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` repository secrets. The client id belongs to the Entra app registration `sunnyscars-api-dev-cicd-01`, which has a federated credential for `repo:sunny-sidhu@39477309/sunnyscars-inventory@1435870092:ref:refs/heads/main` and `AcrPush` on the registry. The repo issues GitHub's immutable OIDC subject, which includes the owner and repo IDs, so the subject has to include them too.
2. Builds the image and pushes it to ACR `crsunnyscarsdev01.azurecr.io` as `sunnyscars-inventory-api:<commit sha>` and `:latest`.
3. If the repository variable `AZURE_WEBAPP_NAME` is set, points that App Service's `main` site container at the `<commit sha>` image (`azure/webapps-deploy` with `sitecontainers-config`; see App Service below) and smoke-tests `GET /v1/cars` (which needs MongoDB too) until it returns 200. Without the variable, both steps are skipped. Until the App Service exists (SCI-7), the job only pushes to ACR.

Deploys share the `deploy-dev` concurrency group, so they never overlap.

## Container image

`SunnysCars.Inventory.Api/Dockerfile` builds from the **repo root** (it needs `global.json`) and only copies `global.json` and the API project. Always build for `linux/amd64`, which is what App Service on Linux runs. On an arm64 Mac, a plain `docker build` would produce an arm64 image that won't start there. The SDK stage cross-compiles natively (`$BUILDPLATFORM` plus `-a $TARGETARCH`) rather than running under emulation.

```sh
docker build --platform linux/amd64 -f SunnysCars.Inventory.Api/Dockerfile -t <registry>.azurecr.io/sunnyscars-inventory-api:<tag> .
az acr build --registry <registry> --platform linux/amd64 -f SunnysCars.Inventory.Api/Dockerfile -t sunnyscars-inventory-api:<tag> .  # or build in ACR
```

Normally the CD job builds and pushes the image, so you only need these commands for a manual build.

The container listens on HTTP port 8080 as a non-root user (`$APP_UID`) and runs as `Production`.

### App Service (SCI-7)

`app-sunnyscars-api-dev-01` runs the image from ACR on Linux, in App Service's *site containers* (sidecar) mode. Its main container is named `main`, uses image `sunnyscars-inventory-api` and port `8080`, and has no startup command (the image's `ENTRYPOINT` starts the app). It starts on `:latest`, and each deploy then pins it to a `<commit sha>` tag. It needs:
- its system-assigned managed identity turned on, with `AcrPull` on `crsunnyscarsdev01`, and the container's registry authentication set to that identity
- a connection string named `Mongo` of type Custom, holding the DocumentDB global read-write string; this becomes `ConnectionStrings:Mongo`
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, so the app sees the original HTTPS scheme from App Service's TLS front end and `UseHttpsRedirection` doesn't misbehave
- network access to the DocumentDB cluster: allow Azure services in the cluster's Networking firewall, or add the App Service's outbound IP addresses

The container's port replaces `WEBSITES_PORT`, which site containers don't use.

For the CD job to deploy to it, give `sunnyscars-api-dev-cicd-01` the `Website Contributor` role on the App Service (or its resource group). Then set the repository variable `AZURE_WEBAPP_NAME` to `app-sunnyscars-api-dev-01`.

The deploy step replaces the whole `main` container definition on every run, so its `sitecontainers-config` in `ci.yml` must match the portal: name `main`, port `8080`, `SystemIdentity` auth, and app settings and connection strings inherited. If you change any of these in the portal, change them in `ci.yml` too, or the next deploy reverts them. Don't switch the step back to `images:`: that converts the app back to a single classic container.

## Architecture

This is an ASP.NET Core minimal API for a car inventory, with CRUD at `/v1/cars` (`Endpoints/CarEndpoints.cs`). Routes are versioned by nesting each resource group under a `/v1` group in `MapCarEndpoints`. Cars are stored in MongoDB: a local server in development, and Azure DocumentDB (MongoDB compatibility) cluster `docdb-sunnyscars-dev-01` in Azure.

- **Storage:** `MongoCarStore` (`Data/MongoCarStore.cs`) is a singleton async `ICarStore` over one collection, one document per car. Ids are ObjectId strings: the store assigns them on insert, and the API exposes them as strings (`/v1/cars/{id}`). An id that isn't a valid ObjectId is treated as not found (404).
- **Document mapping:** a `BsonClassMap<Car>` registered in `MongoCarStore`'s static constructor makes documents match the API's JSON: camelCase fields, `_id` as an ObjectId, `price` as `Decimal128` (older double prices still read), and `transmission` as the lowercase member name.
- **Connection:** `IMongoClient` is a singleton built from `ConnectionStrings:Mongo` (it owns the connection pool). `appsettings.Development.json` points it at `mongodb://localhost:27017`. In other environments, set `ConnectionStrings__Mongo` (in App Service, a connection string named `Mongo` of type Custom). The database and collection come from `CarStore:DatabaseName`/`CollectionName` (default `sunnys_cars`/`inventory`). **POST/PUT/DELETE against a locally running API change whichever MongoDB it points at.**
- **Azure DocumentDB:** use the cluster's *global read-write* connection string, which follows the write region if a replica is ever promoted. Keep its `retrywrites=false`, because DocumentDB doesn't support retryable writes. Never commit the password. To run the API locally against Azure, put the full string in user secrets with `dotnet user-secrets set ConnectionStrings:Mongo "<connection string>" --project SunnysCars.Inventory.Api`; it overrides `appsettings.Development.json` until you run `dotnet user-secrets remove ConnectionStrings:Mongo`. Your IP must be allowed in the cluster's Networking firewall.
- **Seed data:** `SunnysCars.Inventory.Api/Data/cars.json` (no ids) is only read by `scripts/seed-mongodb.js`. That script inserts it into an empty collection and converts any double prices to `Decimal128`, so it's safe to re-run. The API never reads the file. Against Azure, pass the connection string without the password plus `--username <user>`, and mongosh prompts for the password.
- **`Car` vs `CarRequest`:** `Car` (`Models/Car.cs`) is the stored and returned shape. `CarRequest` is the POST/PUT payload: it has no `Id`, carries the validation attributes, and maps to a `Car` via `ToCar(id)`. When you add a field to a car, update `Car`, `CarRequest` (including `ToCar` and the `MapCreator` call in `MongoCarStore`), every entry in `Data/cars.json`, the `.http` samples, and the test fixtures.
- **Validation:** DataAnnotations on `CarRequest` use the `[property: ...]` target so they apply to the generated properties. Failing requests are rejected with a 400 by .NET 10 minimal-API validation (`builder.Services.AddValidation()`) before the handler runs. A required enum field is declared nullable with `[Required, EnumDataType]`, so a missing value fails validation instead of defaulting to the first member (see `Transmission`).
- **JSON:** Serialization uses `JsonSerializerDefaults.Web` (camelCase). Enums serialize as lowercase strings through attributes on the enum itself (`[JsonConverter(typeof(JsonStringEnumConverter<T>))]` plus `[JsonStringEnumMemberName]`), not through global options. That way the API and every serializer call in the tests agree without extra configuration. (MongoDB storage doesn't use `System.Text.Json`; its enum mapping is in `MongoCarStore`'s class map.)

## Tests

`SunnysCars.Inventory.Api.Tests` has unit tests plus one HTTP-level test per endpoint:
- **`CarEndpointsTests`:** runs the whole API in memory with `WebApplicationFactory<Program>`. It swaps in `Fakes/InMemoryCarStore` via `ConfigureTestServices`, so it needs no MongoDB.
- **`MongoCarStoreTests`:** run `MongoCarStore` against a real MongoDB, each test in a throwaway `sunnys_cars_test_<guid>` database that is dropped afterwards. They use `[MongoFact]`, which skips them unless `SUNNYSCARS_TEST_MONGO` holds a connection string.
- **`CarRequestTests`:** runs `Validator.TryValidateObject` on `CarRequest` and asserts which member names fail. Endpoint validation is not exercised.
