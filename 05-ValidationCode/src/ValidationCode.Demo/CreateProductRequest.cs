using System.ComponentModel.DataAnnotations;
using ValidationCode;

namespace ValidationCode.Demo;

[GenerateValidator]
public sealed partial class CreateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 99999)]
    public decimal Price { get; init; }
}
