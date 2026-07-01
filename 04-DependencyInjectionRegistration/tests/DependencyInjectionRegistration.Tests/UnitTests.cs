using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DependencyInjectionRegistration.Generator;

namespace DependencyInjectionRegistration.Tests;

public sealed class DiGeneratorTests
{
    [Fact]
    public void Generates_Registration_For_Single_Interface_Service()
    {
        const string src = """
using DependencyInjectionRegistration;
namespace Demo;
public interface IService {}
[RegisterScoped]
public sealed class Service : IService {}
""";
        var run = Run(src);
        var text = run.Results[0].GeneratedSources.Single(x => x.HintName.Contains("GeneratedServiceCollectionExtensions")).SourceText.ToString();
        Assert.Contains("AddScoped<global::Demo.IService, global::Demo.Service>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_Diagnostic_For_Conflicting_Lifetimes()
    {
        const string src = """
using DependencyInjectionRegistration;
namespace Demo;
[RegisterScoped]
[RegisterSingleton]
public sealed class Service {}
""";
        var run = Run(src);
        Assert.Contains(run.Results[0].Diagnostics, d => d.Id == "DI001");
    }

    private static GeneratorDriverRunResult Run(string source)
    {
        var comp = CSharpCompilation.Create("t", new[] { CSharpSyntaxTree.ParseText(source) }, GetRefs(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new DiRegistrationGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(comp, out _, out _);
        return driver.GetRunResult();
    }

    private static IEnumerable<MetadataReference> GetRefs()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? throw new InvalidOperationException();
        return tpa.Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
    }
}
