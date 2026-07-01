using BoilerplateMapping.Demo;

Product p = new() { Id = Guid.NewGuid(), Name = "Keyboard", Price = 129.00m };
ProductDto dto = p.ToProductDto();
Console.WriteLine($"Mapped: {dto.Name} - {dto.Price}");
