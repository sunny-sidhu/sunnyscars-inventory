using System.Text.Json;
using Microsoft.Extensions.Options;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Data;

public sealed class CarStoreOptions
{
    public const string SectionName = "CarStore";

    /// <summary>
    /// Path to the JSON file holding the cars. Relative paths are resolved against the app's output directory.
    /// </summary>
    public string FilePath { get; set; } = "Data/cars.json";
}

/// <summary>
/// Car store backed by a JSON file. Cars are loaded into memory on startup and every change is written back to the file.
/// </summary>
public sealed class JsonCarStore : ICarStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly Lock _lock = new();
    private readonly List<Car> _cars;

    public JsonCarStore(IOptions<CarStoreOptions> options)
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, options.Value.FilePath);
        _cars = Load(_filePath);
    }

    public IReadOnlyList<Car> GetAll()
    {
        lock (_lock)
        {
            return _cars.ToList();
        }
    }

    public Car? GetById(int id)
    {
        lock (_lock)
        {
            return _cars.FirstOrDefault(c => c.Id == id);
        }
    }

    public Car Add(CarRequest request)
    {
        lock (_lock)
        {
            var nextId = _cars.Count == 0 ? 1 : _cars.Max(c => c.Id) + 1;
            var car = request.ToCar(nextId);
            _cars.Add(car);
            Save();
            return car;
        }
    }

    public Car? Update(int id, CarRequest request)
    {
        lock (_lock)
        {
            var index = _cars.FindIndex(c => c.Id == id);
            if (index < 0)
            {
                return null;
            }

            var car = request.ToCar(id);
            _cars[index] = car;
            Save();
            return car;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            var removed = _cars.RemoveAll(c => c.Id == id) > 0;
            if (removed)
            {
                Save();
            }

            return removed;
        }
    }

    private static List<Car> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<List<Car>>(stream, SerializerOptions) ?? [];
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Write to a temp file first so a failed write can't leave a half-written store behind.
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(_cars, SerializerOptions));
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
