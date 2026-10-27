using System.ComponentModel.DataAnnotations;
using SunnysCars.Inventory.Api.Models;

namespace SunnysCars.Inventory.Api.Tests.Models;

public sealed class CarRequestTests
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var request = new CarRequest("Mazda", "MX-5", "Red", 22500m, 2022, 12000);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Validate_InvalidRequest_ReportsEachInvalidField()
    {
        var request = new CarRequest("", "MX-5", "Red", -1m, 1700, -5);

        var invalidFields = Validate(request).SelectMany(r => r.MemberNames).Order();

        Assert.Equal(["Make", "Mileage", "Price", "Year"], invalidFields);
    }

    private static List<ValidationResult> Validate(CarRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
