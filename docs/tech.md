# Technology Stack

## Framework & Runtime

- **.NET 8 / .NET 9** - Multi-target framework support
- **C# Latest** - Modern language features enabled
- **Nullable reference types** enabled for better type safety
- **Implicit usings** enabled for cleaner code

## Core Dependencies

- **Kentico.Xperience.Core** - Primary Kentico integration (version 30.3.1+)
- **CSharpFunctionalExtensions** - Functional programming utilities (version 3.6.0)
- **Microsoft.Extensions.Logging** - Structured logging support

## Build System

- **MSBuild** with Directory.Build.props for centralized configuration
- **Central Package Management** via Directory.Packages.props
- **Package lock files** enabled for reproducible builds
- **Source Link** integration for debugging support

## Project Structure

- **Library project**: `src/XperienceCommunity.DataContext/`
- **Test project**: `tests/XperienceCommunity.DataContext.Tests/`
- **Solution file**: `XperienceCommunity.DataContext.sln`

## Common Commands

### Build

```bash
dotnet build
dotnet build --configuration Release
```

### Test

```bash
dotnet test
dotnet test --configuration Release --logger trx
```

### Package

```bash
dotnet pack --configuration Release
```

### Restore

```bash
dotnet restore
```

## Development Guidelines

- Use **implicit usings** - common namespaces are automatically included
- Enable **nullable reference types** - all new code should handle nullability properly
- Follow **async/await patterns** for all I/O operations
- Use **dependency injection** for all service registrations
- Implement **IDisposable/IAsyncDisposable** where appropriate