using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using BoilerplateMapping.Generator;

namespace BoilerplateMapping.Tests;

public sealed class MapperGeneratorTests
{
    [Fact]
    public void Generates_Mapping_Method()
    {
        const string src = """
using BoilerplateMapping;
namespace Demo;
public sealed class A { public int Id { get; init; } public string Name { get; init; } = string.Empty; }
public sealed class B { public int Id { get; init; } public string Name { get; init; } = string.Empty; }
[GenerateMapper(typeof(A), typeof(B))]
public static partial class M {}
""";
        var run = Run(src);
        var text = run.Results[0].GeneratedSources.Single(x => x.HintName.Contains("A_to_B")).SourceText.ToString();
        Assert.Contains("ToB", text, StringComparison.Ordinal);
        Assert.Contains("Id = source.Id", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_NoMatchingProperties()
    {
        const string src = """
using BoilerplateMapping;
namespace Demo;
public sealed class A { public int Id { get; init; } }
public sealed class B { public string Name { get; init; } = string.Empty; }
[GenerateMapper(typeof(A), typeof(B))]
public static partial class M {}
""";
        var run = Run(src);
        Assert.Contains(run.Results[0].Diagnostics, d => d.Id == "MAP003");
    }

    private static GeneratorDriverRunResult Run(string source)
    {
        var comp = CSharpCompilation.Create("t", new[] { CSharpSyntaxTree.ParseText(source) }, GetRefs(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new MapperGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(comp, out _, out _);
        return driver.GetRunResult();
    }

    private static IEnumerable<MetadataReference> GetRefs()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? throw new InvalidOperationException();
        return tpa.Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));
    }
}
