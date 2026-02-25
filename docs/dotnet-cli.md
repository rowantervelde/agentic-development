# .NET CLI Commands

## Templates

List all available templates:

```bash
dotnet new list
```

Filter templates by name:

```bash
dotnet new list asp.net
```

## New Project / Solution

```bash
dotnet new sln -n CareMetrics
dotnet new webapi -n CareMetrics.API
dotnet sln CareMetrics.slnx add CareMetrics.API/CareMetrics.API.csproj
```

## Packages

### Install a Package

```bash
dotnet add ./CareMetrics.API.csproj package Swashbuckle.AspNetCore
```

### Update a Package

In VS Code, use the NuGet extension:

`F1` → **NuGet: Update NuGet Package...**

## Run

Run using a specific launch profile:

```bash
dotnet run --launch-profile https
```

## Swagger / OpenAPI

The only configuration needed is the built-in OpenAPI support.
See: [Use the generated OpenAPI documents | Microsoft Learn](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/using-openapi-documents)

| Endpoint     | URL                                      |
| ------------ | ---------------------------------------- |
| OpenAPI JSON | `https://localhost:7185/openapi/v1.json` |
| Swagger UI   | `https://localhost:7185/swagger/`        |
