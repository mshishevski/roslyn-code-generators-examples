using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ReflectionFreeSerialization.Generator;

namespace ReflectionFreeSerialization.Tests;

public sealed class ReflectionFreeSerializationGeneratorTests
{
    [Fact]
    public void Generates_Context_For_Annotated_Types()
    {
        const string source = """
            using System;
            using ReflectionFreeSerialization;

            namespace Demo;

            [GenerateJsonSerializable]
            public sealed record CreateOrderRequest(Guid CustomerId, decimal Amount);
            """;

        GeneratorDriverRunResult result = RunGenerator(source);
        string generatedContext = result.Results[0].GeneratedSources
            .Single(item => item.HintName == "GeneratedJsonContext.g.cs")
            .SourceText
            .ToString();

        Assert.Contains("public sealed partial class GeneratedJsonContext : JsonSerializerContext", generatedContext, StringComparison.Ordinal);
        Assert.Contains("JsonTypeInfo<global::Demo.CreateOrderRequest>", generatedContext, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_Not_Generate_Context_When_No_Annotated_Types_Exist()
    {
        const string source = """
            namespace Demo;

            public sealed record CreateOrderRequest(System.Guid CustomerId, decimal Amount);
            """;

        GeneratorDriverRunResult result = RunGenerator(source);

        bool hasContext = result.Results[0].GeneratedSources.Any(static item =>
            item.HintName.Equals("GeneratedJsonContext.g.cs", StringComparison.Ordinal));

        Assert.False(hasContext);
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator generator = new ReflectionFreeSerializationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
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
}
