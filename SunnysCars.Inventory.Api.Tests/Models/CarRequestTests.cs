using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Tests.Models;

public sealed class CarRequestTests
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var request = new CarRequest("Mazda", "MX-5", "Red", 22500m, 2022, 12000, Transmission.Manual);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_InvalidRequest_ReportsEachInvalidField()
    {
        var request = new CarRequest("", "MX-5", "Red", -1m, 1700, -5, null);

        var invalidFields = Validate(request).SelectMany(r => r.MemberNames).Order();

        Assert.Equal(["Make", "Mileage", "Price", "Transmission", "Year"], invalidFields);
    }

    [Fact]
    public void Validate_UndefinedTransmission_ReportsTransmission()
    {
        var request = new CarRequest("Mazda", "MX-5", "Red", 22500m, 2022, 12000, (Transmission)42);

        var invalidFields = Validate(request).SelectMany(r => r.MemberNames);

        Assert.Equal(["Transmission"], invalidFields);
    }

    [Theory]
    [InlineData("\"automatic\"", Transmission.Automatic)]
    [InlineData("\"manual\"", Transmission.Manual)]
    public void Transmission_DeserializesFromLowercaseString(string json, Transmission expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<Transmission>(json));
    }

    [Fact]
    public void Transmission_UnknownString_FailsToDeserialize()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Transmission>("\"cvt\""));
    }

    private static List<ValidationResult> Validate(CarRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
