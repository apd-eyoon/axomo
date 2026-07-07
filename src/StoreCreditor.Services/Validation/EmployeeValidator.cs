using System.Net.Mail;
using StoreCreditor.Data.Entities;

namespace StoreCreditor.Services.Validation;

public sealed class EmployeeValidator : IEmployeeValidator
{
    public EmployeeValidationResult Validate(EmployeeStaging employee)
    {
        if (string.IsNullOrWhiteSpace(employee.EmployeeId))
        {
            return EmployeeValidationResult.Failure("Employee ID is required.");
        }

        if (!employee.IsActive)
        {
            return EmployeeValidationResult.Failure("Employee is inactive.");
        }

        if (string.IsNullOrWhiteSpace(employee.Email))
        {
            return EmployeeValidationResult.Failure("Employee email is required.");
        }

        try
        {
            _ = new MailAddress(employee.Email);
        }
        catch (FormatException)
        {
            return EmployeeValidationResult.Failure("Employee email is invalid.");
        }

        return EmployeeValidationResult.Success;
    }
}
