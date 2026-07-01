using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using StronglyTypedApiClient.Demo.Generated;

HttpClient httpClient = new(new FakeBillingHandler())
{
    BaseAddress = new Uri("https://localhost")
};

IBillingApiClient client = new BillingApiClient(httpClient);
CreateInvoiceResponse response = await client.CreateInvoiceAsync(
    new CreateInvoiceRequest
    {
        CustomerId = Guid.Parse("405b8b0d-2be2-4a18-84d1-0fcf9e2a1947"),
        Amount = 89.95m,
        Currency = "USD"
    });

Console.WriteLine($"Invoice created: {response.InvoiceId}, status: {response.Status}");

file sealed class FakeBillingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(new
        {
            invoiceId = Guid.Parse("7f6df487-7698-4dea-97f3-8f6f11f7ac06"),
            status = "Created"
        });

        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}
