using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ValidationCode.Generator;

namespace ValidationCode.Tests;

public sealed class ValidationGeneratorTests
{
    [Fact]
    public void Generates_Validator_For_GenerateValidator_Model()
    {
        const string src = """
using ValidationCode;
using System.ComponentModel.DataAnnotations;
namespace Demo;
[GenerateValidator]
public sealed class R { [Required] public string Name { get; init; } = string.Empty; }
""";
        var run = Run(src);
        var text = run.Results[0].GeneratedSources.Single(x => x.HintName.Contains("R.Validator")).SourceText.ToString();
        Assert.Contains("public static partial class RValidator", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_Diagnostic_For_Unsupported_Attribute()
    {
        const string src = """
using ValidationCode;
namespace Demo;
[GenerateValidator]
public sealed class R { [Obsolete] public string Name { get; init; } = string.Empty; }
""";
        var run = Run(src);
        Assert.Contains(run.Results[0].Diagnostics, d => d.Id == "VAL001");
    }

    private static GeneratorDriverRunResult Run(string source)
    {
        var comp = CSharpCompilation.Create("t", new[] { CSharpSyntaxTree.ParseText(source) }, GetRefs(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ValidationGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(comp, out _, out _);
        return driver.GetRunResult();
    }

    private static IEnumerable<MetadataReference> GetRefs()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? throw new InvalidOperationException();
        return tpa.Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
    }
}
