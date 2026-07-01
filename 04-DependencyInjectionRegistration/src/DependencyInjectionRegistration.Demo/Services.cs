using DependencyInjectionRegistration;

namespace DependencyInjectionRegistration.Demo;

public interface IInvoiceService { string CreateInvoice(); }

[RegisterScoped]
public sealed class InvoiceService : IInvoiceService
{
    public string CreateInvoice() => "created";
}
