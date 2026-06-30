namespace ReflectionFreeSerialization.Demo;

[GenerateJsonSerializable]
public sealed record CreateOrderRequest(Guid CustomerId, decimal Amount);
