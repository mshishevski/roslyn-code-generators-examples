using BoilerplateMapping;

namespace BoilerplateMapping.Demo;

[GenerateMapper(typeof(Product), typeof(ProductDto))]
public static partial class ProductMapper
{
}
