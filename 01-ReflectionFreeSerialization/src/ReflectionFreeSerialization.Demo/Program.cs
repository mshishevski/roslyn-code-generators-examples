using System.Text.Json;
using ReflectionFreeSerialization.Demo;
using ReflectionFreeSerialization.Demo.Generated;

CreateOrderRequest request = new(Guid.Parse("1a7dad2c-13d8-4626-bf70-c58f73b28812"), 125.50m);

var json = JsonSerializer.Serialize(request, GeneratedJsonContext.Default.CreateOrderRequest);
CreateOrderRequest? roundTrip = JsonSerializer.Deserialize(json, GeneratedJsonContext.Default.CreateOrderRequest);

Console.WriteLine($"Serialized payload: {json}");
Console.WriteLine($"Round-trip amount: {roundTrip?.Amount}");
