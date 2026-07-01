# 05 - Validation Code

Generates strongly typed validators from `[GenerateValidator]` models and DataAnnotations.

Supported attributes:
- `[Required]`
- `[StringLength]`
- `[Range]`

Unsupported attributes on marked models emit diagnostic `VAL001`.

Run:
```powershell
dotnet build ValidationCode.sln
dotnet test ValidationCode.sln
dotnet run --project src/ValidationCode.Demo/ValidationCode.Demo.csproj
```
