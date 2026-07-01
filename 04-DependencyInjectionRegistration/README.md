# 04 - Dependency Injection Registration

Generates `AddGeneratedServices` from compile-time attributes:
- `[RegisterScoped]`
- `[RegisterSingleton]`
- `[RegisterTransient]`

Rules:
- Exactly one non-framework interface => register interface-to-implementation.
- No interface => register concrete type.
- Conflicting lifetime attributes => diagnostic `DI001`.

Run:
```powershell
dotnet build DependencyInjectionRegistration.sln
dotnet test DependencyInjectionRegistration.sln
dotnet run --project src/DependencyInjectionRegistration.Demo/DependencyInjectionRegistration.Demo.csproj
```
