# Roslyn Generator Examples

A repository with six independent .NET solutions showing realistic, production-oriented Roslyn incremental source generator use cases.

## Why This Repository Exists

This repo is designed to demonstrate:

- incremental source generator architecture
- deterministic compile-time code generation
- explicit diagnostics and failure modes
- generator testing with Roslyn APIs
- practical build/review workflows for generated code

## What Roslyn Source Generators Are

Roslyn source generators run during compilation and can emit additional C# source files based on code and deterministic inputs.

This allows teams to:

- remove repetitive boilerplate
- shift failures left to compile time
- reduce runtime reflection and startup cost
- improve AOT and trimming compatibility

## Why Incremental Generators

All examples use `IIncrementalGenerator` instead of older non-incremental patterns.

Incremental generators are preferred because they:

- cache intermediate results
- re-run only affected pipelines
- produce faster and more stable developer builds
- align better with large codebases and CI

## Solutions
- `01-ReflectionFreeSerialization`
- `02-StronglyTypedApiClient`
- `03-BoilerplateMapping`
- `04-DependencyInjectionRegistration`
- `05-ValidationCode`

Each solution is fully independent and contains:

- a generator project
- a demo app
- a test project
- a solution-specific README

## Build And Run

From this repository root:

```powershell
# 01
dotnet build .\01-ReflectionFreeSerialization\ReflectionFreeSerialization.sln
dotnet test  .\01-ReflectionFreeSerialization\ReflectionFreeSerialization.sln
dotnet run --project .\01-ReflectionFreeSerialization\src\ReflectionFreeSerialization.Demo\ReflectionFreeSerialization.Demo.csproj

# 02
dotnet build .\02-StronglyTypedApiClient\StronglyTypedApiClient.sln
dotnet test  .\02-StronglyTypedApiClient\StronglyTypedApiClient.sln
dotnet run --project .\02-StronglyTypedApiClient\src\StronglyTypedApiClient.Demo\StronglyTypedApiClient.Demo.csproj

# 03
dotnet build .\03-BoilerplateMapping\BoilerplateMapping.sln
dotnet test  .\03-BoilerplateMapping\BoilerplateMapping.sln
dotnet run --project .\03-BoilerplateMapping\src\BoilerplateMapping.Demo\BoilerplateMapping.Demo.csproj

# 04
dotnet build .\04-DependencyInjectionRegistration\DependencyInjectionRegistration.sln
dotnet test  .\04-DependencyInjectionRegistration\DependencyInjectionRegistration.sln
dotnet run --project .\04-DependencyInjectionRegistration\src\DependencyInjectionRegistration.Demo\DependencyInjectionRegistration.Demo.csproj

# 05
dotnet build .\05-ValidationCode\ValidationCode.sln
dotnet test  .\05-ValidationCode\ValidationCode.sln
dotnet run --project .\05-ValidationCode\src\ValidationCode.Demo\ValidationCode.Demo.csproj

```

## How To Inspect Generated Code

Demo projects enable:

- `EmitCompilerGeneratedFiles=true`

Generated files are emitted under each demo project's `obj/Generated` directory.

No generator in this repository makes network calls. No AI model is invoked during compilation.
