using StoreCreditor.Data.Entities;

namespace StoreCreditor.Services.Validation;

public interface IEmployeeValidator
{
    EmployeeValidationResult Validate(EmployeeStaging employee);
}
