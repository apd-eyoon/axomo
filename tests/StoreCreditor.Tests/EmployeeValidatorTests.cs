using StoreCreditor.Data.Entities;
using StoreCreditor.Services.Validation;

namespace StoreCreditor.Tests;

public sealed class EmployeeValidatorTests
{
    [Fact]
    public void Validate_ReturnsSuccess_ForActiveEmployeeWithValidEmail()
    {
        var validator = new EmployeeValidator();
        var employee = new EmployeeStaging
        {
            EmployeeId = "123",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada.lovelace@aimpointdigital.com"
        };

        var result = validator.Validate(employee);

        Assert.True(result.IsValid);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void Validate_ReturnsFailure_ForInvalidEmail()
    {
        var validator = new EmployeeValidator();
        var employee = new EmployeeStaging
        {
            EmployeeId = "123",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "not-an-email"
        };

        var result = validator.Validate(employee);

        Assert.False(result.IsValid);
        Assert.Equal("Employee email is invalid.", result.FailureReason);
    }
}
