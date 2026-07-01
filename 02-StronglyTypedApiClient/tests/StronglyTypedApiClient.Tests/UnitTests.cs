using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using StronglyTypedApiClient.Generator;

namespace StronglyTypedApiClient.Tests;

public sealed class StronglyTypedApiClientGeneratorTests
{
    [Fact]
    public void Generates_Client_Dtos_And_Interface_From_Contract()
    {
        const string contractJson = """
            {
              "clientName": "BillingApiClient",
              "namespace": "Demo.Generated",
              "endpoints": [
                {
                  "name": "CreateInvoice",
                  "method": "POST",
                  "route": "/api/invoices",
                  "request": {
                    "name": "CreateInvoiceRequest",
                    "properties": {
                      "customerId": "Guid",
                      "amount": "decimal"
                    }
                  },
                  "response": {
                    "name": "CreateInvoiceResponse",
                    "properties": {
                      "invoiceId": "Guid",
                      "status": "string"
                    }
                  }
                }
              ]
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(contractJson);
        string generated = result.Results[0].GeneratedSources.Single().SourceText.ToString();

        Assert.Contains("public interface IBillingApiClient", generated, StringComparison.Ordinal);
        Assert.Contains("Task<CreateInvoiceResponse> CreateInvoiceAsync", generated, StringComparison.Ordinal);
        Assert.Contains("public sealed class CreateInvoiceRequest", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_Diagnostic_When_ClientName_Is_Missing()
    {
        const string contractJson = """
            {
              "namespace": "Demo.Generated",
              "endpoints": []
            }
            """;

        GeneratorDriverRunResult result = RunGenerator(contractJson);
        ImmutableArray<Diagnostic> diagnostics = result.Results[0].Diagnostics;

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "STA002");
    }

    private static GeneratorDriverRunResult RunGenerator(string contractJson)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText("namespace Demo; public sealed class Marker {}") },
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ISourceGenerator generator = new StronglyTypedApiClientGenerator().AsSourceGenerator();
        AdditionalText contractFile = new InMemoryAdditionalText("api-contract.json", contractJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            additionalTexts: ImmutableArray.Create(contractFile),
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees[0].Options);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }

    private static IEnumerable<MetadataReference> GetReferences()
    {
        string trustedAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)
            ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is unavailable.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(content);
        }
    }
}
