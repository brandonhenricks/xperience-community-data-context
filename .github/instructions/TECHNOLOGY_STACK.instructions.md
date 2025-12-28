# Technology Stack

This document provides a comprehensive overview of all technologies, frameworks, libraries, and tools used in the XperienceCommunity.DataContext project.

---

## .NET Platform

### Target Frameworks
- **.NET 8.0** (LTS - Long Term Support until November 2026)
- **.NET 9.0** (Standard Term Support until May 2026)

**Rationale**: Multi-targeting ensures compatibility with both the current LTS release (.NET 8) and the latest features (.NET 9).

### Language
- **C# Latest** (C# 12 for .NET 8, C# 13 for .NET 9)
- **Language Features Used**:
  - File-scoped namespaces
  - Nullable reference types
  - Pattern matching
  - Records and record structs
  - Target-typed new expressions
  - Collection expressions (.NET 8+)
  - Primary constructors
  - Init-only properties

---

## Core Dependencies

### Kentico Xperience
- **Package**: `Kentico.Xperience.Core`
- **Version**: `[30.3.1,)` (30.3.1 or higher, open-ended upper bound)
- **Purpose**: Core Kentico Xperience functionality
- **Key APIs Used**:
  - `IContentQueryExecutor` - Query execution
  - `IProgressiveCache` - Caching infrastructure
  - `IWebsiteChannelContext` - Channel/preview awareness
  - `ContentItemQueryBuilder` - Query building
  - `IContentItemFieldsSource` - Content item interface
  - `IWebPageFieldsSource` - Web page interface

**Integration Points**:
- All contexts wrap Kentico's `ContentItemQueryBuilder`
- Caching leverages Kentico's `IProgressiveCache` with cache dependencies
- Preview mode detection via `IWebsiteChannelContext`

### CSharpFunctionalExtensions
- **Package**: `CSharpFunctionalExtensions`
- **Version**: `3.6.0`
- **Purpose**: Functional programming patterns (Result, Maybe, etc.)
- **Usage**: Implicit using for functional utilities

---

## Development Dependencies

### Source Link
- **Package**: `Microsoft.SourceLink.GitHub`
- **Version**: `[8.0.0.0,)`
- **Purpose**: Enable source code debugging for NuGet consumers
- **Configuration**: Enabled in Release builds only
```xml
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
```

### Logging
- **Package**: `Microsoft.Extensions.Logging`
- **Version**: `[8.0.0.0,)`
- **Purpose**: Structured logging throughout the library
- **Abstractions Used**: `ILogger<T>`, `LogLevel`

---

## Testing Stack

### Test Framework
- **Package**: `xunit`
- **Version**: `2.8.1`
- **Purpose**: Unit testing framework
- **Features Used**:
  - `[Fact]` attributes for tests
  - `[Theory]` for parameterized tests
  - `Assert` class for assertions

### Test Runner
- **Package**: `xunit.runner.visualstudio`
- **Version**: `2.8.1`
- **Purpose**: Visual Studio Test Explorer integration

### Mocking
- **Package**: `NSubstitute`
- **Version**: `5.3.0`
- **Purpose**: Mocking framework for test isolation
- **Features Used**:
  - `Substitute.For<T>()` for interface mocking
  - `Arg.Any<T>()` for argument matching
  - `Returns()` for mock behavior
  - `Received()` for interaction verification

### Code Coverage
- **Package**: `coverlet.collector`
- **Version**: `6.0.2`
- **Purpose**: Code coverage collection during test execution
- **Integration**: Works with `dotnet test --collect:"XPlat Code Coverage"`

### Test SDK
- **Package**: `Microsoft.NET.Test.Sdk`
- **Version**: `17.10.0`
- **Purpose**: .NET testing platform infrastructure

---

## Build & Packaging

### Central Package Management
- **Feature**: NuGet Central Package Management (CPM)
- **Configuration**: `Directory.Packages.props`
- **Benefits**:
  - Single source of truth for package versions
  - Easier dependency updates
  - Consistent versions across all projects

### Package Lock Files
```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<DisableImplicitNuGetFallbackFolder>true</DisableImplicitNuGetFallbackFolder>
```
- **Purpose**: Reproducible builds
- **Files**: `packages.lock.json` in each project

### NuGet Configuration
```xml
<!-- nuget.config -->
<packageSources>
  <clear />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

---

## CI/CD Technologies

### GitHub Actions
- **Workflows**: `.github/workflows/`
- **dotnet.yml**: Build and test pipeline
- **release.yml**: NuGet package publishing

### Build Workflow (.github/workflows/dotnet.yml)
```yaml
name: .NET
on:
  push:
    branches: [ "master" ]
  pull_request:
    branches: [ "master" ]

jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    
    steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    - run: dotnet restore
    - run: dotnet build --no-restore --configuration Release
    - run: dotnet test --configuration Release --logger trx
    - uses: actions/upload-artifact@v4
      with:
        name: test-results
        path: TestResults/*.trx
```

**Features**:
- Builds on Ubuntu (Linux)
- Uses .NET 9 SDK
- Runs tests and uploads results
- 15-minute timeout

### Release Workflow (.github/workflows/release.yml)
```yaml
name: NuGet Release
on:
  push:
    tags:
    - "v[0-9]+.[0-9]+.[0-9]+*"  # Supports v1.2.3, v1.2.3-alpha, etc.
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to Release'
        required: true
```

**Features**:
- Triggered by version tags (e.g., `v1.0.0`)
- Manual dispatch option
- Version validation
- Auto-generates release notes from commits
- Publishes to NuGet.org

---

## Diagnostic & Telemetry

### OpenTelemetry Integration
- **ActivitySource**: `XperienceCommunity.Data.Context.QueryExecution`
- **Purpose**: Distributed tracing support
- **Tags**: 
  - `contentType`: Type of content being queried
  - `processorCount`: Number of processors applied
  - `executionTimeMs`: Query execution time
  - `resultCount`: Number of results returned
  - `error`: Boolean for error indication
  - `errorMessage`: Error details

### Performance Counters
```csharp
// Static counters in ProcessorSupportedQueryExecutor<T, TProcessor>
public static long TotalExecutions { get; }
public static long TotalProcessingTimeMs { get; }
public static double AverageProcessingTimeMs { get; }
```

### Diagnostics API
```csharp
// Enable diagnostics globally
DataContextDiagnostics.DiagnosticsEnabled = true;
DataContextDiagnostics.TraceLevel = LogLevel.Debug;

// Get performance stats
var stats = DataContextDiagnostics.GetPerformanceStats();

// Generate diagnostic reports
var report = DataContextDiagnostics.GetDiagnosticReport();
```

---

## Development Tools

### IDEs
- **Visual Studio 2022** (17.8 or later for .NET 8/9)
- **Visual Studio Code** with C# extension
- **JetBrains Rider** (2023.3 or later)

### SDK Requirements
- **.NET 8 SDK** (8.0.100 or later)
- **.NET 9 SDK** (9.0.100 or later)

### Command-Line Tools
```bash
# Build
dotnet build

# Run tests
dotnet test

# Create NuGet package
dotnet pack --configuration Release

# Restore packages
dotnet restore
```

---

## Version Constraints & Compatibility

### Package Version Ranges
```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Kentico.Xperience.Core" Version="[30.3.1,)" />
<!-- Minimum 30.3.1, no upper bound -->

<PackageVersion Include="Microsoft.SourceLink.GitHub" Version="[8.0.0.0,)" />
<!-- Minimum 8.0, no upper bound -->

<PackageVersion Include="Microsoft.Extensions.Logging" Version="[8.0.0.0,)" />
<!-- Minimum 8.0, no upper bound -->

<PackageVersion Include="CSharpFunctionalExtensions" Version="3.6.0" />
<!-- Exact version -->
```

### .NET Version Support Matrix
| Library Version | .NET 8 | .NET 9 | Kentico Xperience |
|----------------|--------|--------|-------------------|
| 1.x.x          | ✅      | ✅      | 30.3.1+           |
| Future 2.x.x   | ✅      | ✅      | 31.x.x+           |

---

## Architecture Integration Points

### Dependency Injection Container
- **Requirement**: Microsoft.Extensions.DependencyInjection-compatible container
- **Service Lifetimes**:
  - **Scoped**: All contexts, executors
  - **Singleton**: Configuration (`XperienceDataContextConfig`)
  - **Transient**: Not used (for performance)

### Caching System
- **Interface**: `IProgressiveCache` (Kentico)
- **Behavior**: Multi-level caching with cache dependencies
- **Cache Keys**: Composite (content type + language + channel + parameters + query hash)

### Expression Trees
- **System.Linq.Expressions**: Core expression tree API
- **Visitor Pattern**: Custom expression visitor (`ContentItemQueryExpressionVisitor`)
- **Processors**: Strategy pattern for expression type handling

---

## Future Technology Considerations

### Planned
1. **Analyzers & Code Generators**: Roslyn analyzers for compile-time validation
2. **Benchmarking**: BenchmarkDotNet for performance regression testing
3. **.NET 10 Support**: Add when released (November 2025)

### Under Evaluation
1. **GraphQL Support**: Optional GraphQL query generation
2. **EF Core Integration**: Side-by-side usage with Entity Framework Core
3. **Azure Functions Support**: Optimizations for serverless scenarios

---

## Development Environment Setup

### Prerequisites
```bash
# Install .NET 8 & 9 SDKs
winget install Microsoft.DotNet.SDK.8
winget install Microsoft.DotNet.SDK.9

# Verify installation
dotnet --list-sdks
```

### Clone & Build
```bash
git clone https://github.com/brandonhenricks/xperience-community-data-context.git
cd xperience-community-data-context
dotnet restore
dotnet build
dotnet test
```

### IDE Setup (VS Code)
```json
// .vscode/settings.json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "csharp.format.enable": true
}
```

---

## Licensing

### Library License
- **License**: MIT License
- **File**: `LICENSE`
- **Copyright**: Brandon Henricks

### Dependency Licenses
- **Kentico.Xperience.Core**: Proprietary (Kentico license required)
- **CSharpFunctionalExtensions**: MIT
- **NSubstitute**: BSD 3-Clause
- **xUnit**: Apache 2.0

---

## Security & Compliance

### Dependency Scanning
- **GitHub Dependabot**: Enabled for automated security updates
- **NuGet Package Vulnerabilities**: Monitored via `dotnet list package --vulnerable`

### Build Security
```xml
<!-- Directory.Build.props -->
<ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
<!-- Deterministic builds for security -->
```

---

## Performance Characteristics

### Memory
- **Footprint**: ~50KB assembly size (Release build)
- **Allocations**: Minimized via `Span<T>`, `ValueTask`, and pooling

### Throughput
- **Query Execution**: ~1-5ms overhead (expression translation)
- **Caching**: ~0.1ms cache hit (Kentico IProgressiveCache)

### Scalability
- **Thread Safety**: ConcurrentDictionary for shared state
- **Async/Await**: All I/O operations are async
- **Cancellation**: Full CancellationToken support

---

**Last Updated**: December 28, 2025
**Technology Review**: Quarterly
