# 03 - Boilerplate Mapping

Generates mapping extension methods from `[GenerateMapper(typeof(Source), typeof(Target))]` declarations.

- Matches public readable source properties to public writable/init target properties.
- Emits diagnostics for missing source, missing target, and no matching properties.
- Demo maps `Product` to `ProductDto`.

Run:

```powershell
dotnet build BoilerplateMapping.sln
dotnet test BoilerplateMapping.sln
dotnet run --project src/BoilerplateMapping.Demo/BoilerplateMapping.Demo.csproj
```
