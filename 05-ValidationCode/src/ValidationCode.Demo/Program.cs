using ValidationCode.Demo;
using ValidationCode.Demo.Generated;

CreateProductRequest valid = new() { Name = "Desk", Price = 299.99m };
CreateProductRequest invalid = new() { Name = "A", Price = -1m };

var validResult = CreateProductRequestValidator.Validate(valid);
var invalidResult = CreateProductRequestValidator.Validate(invalid);

Console.WriteLine($"Valid result: {validResult.IsValid}");
Console.WriteLine($"Invalid result: {invalidResult.IsValid}, errors: {invalidResult.Errors.Count}");
