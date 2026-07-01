using DependencyInjectionRegistration.Demo;
using DependencyInjectionRegistration.Demo.Generated;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
services.AddGeneratedServices();
using ServiceProvider provider = services.BuildServiceProvider();
IInvoiceService svc = provider.GetRequiredService<IInvoiceService>();
Console.WriteLine(svc.CreateInvoice());
