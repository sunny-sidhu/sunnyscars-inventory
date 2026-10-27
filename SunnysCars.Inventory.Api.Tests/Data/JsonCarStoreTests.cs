using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SunnysCars.Inventory.Api.Data;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Tests.Data;

public sealed class JsonCarStoreTests : IDisposable
{
    private const string FileName = "cars.json";

    private static readonly Car[] SeedCars =
    [
        new(1, "Toyota", "Corolla", "Silver", 14995m, 2019, 42000, Transmission.Automatic),
        new(2, "Ford", "Focus", "Blue", 11250m, 2018, 56300, Transmission.Manual)
    ];

    private static readonly CarRequest NewCar = new("Mazda", "MX-5", "Red", 22500m, 2022, 12000, Transmission.Manual);

    private readonly string _contentRoot = Directory.CreateTempSubdirectory("sunnyscars-tests-").FullName;

    public void Dispose() => Directory.Delete(_contentRoot, recursive: true);

    [Fact]
    public void GetAll_ReturnsCarsFromFile()
    {
        var store = CreateStore(SeedCars);

        Assert.Equal(SeedCars, store.GetAll());
    }

    [Fact]
    public void GetById_ExistingId_ReturnsCar()
    {
        var store = CreateStore(SeedCars);

        Assert.Equal(SeedCars[1], store.GetById(2));
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        var store = CreateStore(SeedCars);

        Assert.Null(store.GetById(999));
    }

    [Fact]
    public void Add_AssignsNextIdAndPersistsToFile()
    {
        var store = CreateStore(SeedCars);

        var added = store.Add(NewCar);

        Assert.Equal(NewCar.ToCar(3), added);
        Assert.Equal(added, CreateStore().GetById(3));
    }

    [Fact]
    public void Add_WhenFileMissing_StartsIdsAtOne()
    {
        var store = CreateStore();

        var added = store.Add(NewCar);

        Assert.Equal(1, added.Id);
        Assert.True(File.Exists(Path.Combine(_contentRoot, FileName)));
    }

    [Fact]
    public void Update_ExistingId_ReplacesCarAndPersistsToFile()
    {
        var store = CreateStore(SeedCars);

        var updated = store.Update(1, NewCar);

        Assert.Equal(NewCar.ToCar(1), updated);
        Assert.Equal(updated, CreateStore().GetById(1));
    }

    [Fact]
    public void Update_UnknownId_ReturnsNullAndLeavesStoreUnchanged()
    {
        var store = CreateStore(SeedCars);

        Assert.Null(store.Update(999, NewCar));
        Assert.Equal(SeedCars, store.GetAll());
    }

    [Fact]
    public void Delete_ExistingId_RemovesCarAndPersistsToFile()
    {
        var store = CreateStore(SeedCars);

        Assert.True(store.Delete(1));
        Assert.Null(store.GetById(1));
        Assert.Equal([SeedCars[1]], CreateStore().GetAll());
    }

    [Fact]
    public void Add_PersistsTransmissionAsLowercaseString()
    {
        var store = CreateStore();

        store.Add(NewCar);

        var json = File.ReadAllText(Path.Combine(_contentRoot, FileName));
        Assert.Contains("\"transmission\": \"manual\"", json);
    }

    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        var store = CreateStore(SeedCars);

        Assert.False(store.Delete(999));
        Assert.Equal(SeedCars, store.GetAll());
    }

    /// <summary>
    /// Creates a store over the temp content root, optionally writing <paramref name="seed"/> to its file first.
    /// </summary>
    private JsonCarStore CreateStore(Car[]? seed = null)
    {
        if (seed is not null)
        {
            File.WriteAllText(
                Path.Combine(_contentRoot, FileName),
                JsonSerializer.Serialize(seed, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }

        var options = Options.Create(new CarStoreOptions { FilePath = FileName });
        return new JsonCarStore(options, new TestHostEnvironment(_contentRoot));
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "SunnysCars.Inventory.Api.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
