# Project Folder Structure

```
xperience-community-data-context/
│
├── .github/                              # GitHub-specific configurations
│   ├── chatmodes/                        # AI assistant chat modes
│   │   └── custom_instructions.chatmode.md
│   ├── instructions/                     # Documentation (this directory)
│   │   ├── ARCHITECTURE.instructions.md
│   │   ├── CODE_EXEMPLARS.instructions.md
│   │   ├── CODING_STANDARDS.instructions.md
│   │   ├── PROJECT_FOLDER_STRUCTURE.instructions.md (this file)
│   │   ├── TECHNOLOGY_STACK.instructions.md
│   │   ├── UNIT_TESTS.instructions.md
│   │   └── WORKFLOW_ANALYSIS.instructions.md
│   ├── workflows/                        # GitHub Actions CI/CD
│   │   ├── dotnet.yml                    # Build & test workflow
│   │   └── release.yml                   # NuGet package release workflow
│   └── copilot-instructions.md           # GitHub Copilot configuration
│
├── docs/                                 # Project documentation
│   ├── Debugging-Guide.md                # Comprehensive debugging guide
│   ├── product.md                        # Product documentation
│   ├── structure.md                      # Architecture documentation
│   └── tech.md                           # Technical details
│
├── images/                               # Project images and assets
│   └── logo.png                          # Package logo
│
├── src/                                  # Source code (main library)
│   └── XperienceCommunity.DataContext/
│       ├── Abstractions/                 # Interfaces and contracts
│       │   ├── IContentItemContext.cs    # Content item context interface
│       │   ├── IDataContext.cs           # Base data context interface
│       │   ├── IExpressionContext.cs     # Expression context interface
│       │   ├── IPageContentContext.cs    # Page content context interface
│       │   ├── IReusableSchemaContext.cs # Reusable schema context interface
│       │   ├── IXperienceDataContext.cs  # Main factory interface
│       │   └── Processors/               # Processor interfaces
│       │       ├── IContentItemProcessor.cs (marker interface)
│       │       ├── IExpressionProcessor.cs  # Expression processor interface
│       │       ├── IPageContentProcessor.cs (marker interface)
│       │       └── IProcessor.cs            # Base processor interface
│       │
│       ├── Configurations/               # Configuration classes
│       │   ├── XperienceContextBuilder.cs    # Fluent builder for DI configuration
│       │   └── XperienceDataContextConfig.cs # Cache and settings configuration
│       │
│       ├── Contexts/                     # Context implementations
│       │   ├── ContentItemContext.cs     # Content hub item queries
│       │   ├── ExpressionContext.cs      # Expression translation state management
│       │   ├── PageContentContext.cs     # Web page queries
│       │   ├── ReusableSchemaContext.cs  # Reusable schema queries
│       │   └── XperienceDataContext.cs   # Main context factory implementation
│       │
│       ├── Core/                         # Base classes and abstractions
│       │   ├── BaseContentQueryExecutor.cs           # Abstract query executor
│       │   ├── BaseDataContext.cs                    # Abstract data context (70% code reuse)
│       │   └── ProcessorSupportedQueryExecutor.cs    # Executor with processor support
│       │
│       ├── Diagnostics/                  # Diagnostics and telemetry
│       │   ├── DataContextDiagnostics.cs # Static diagnostics manager
│       │   └── DiagnosticEntry.cs        # Diagnostic event entry
│       │
│       ├── Exceptions/                   # Custom exception types
│       │   ├── ExpressionProcessingException.cs  # Expression translation errors
│       │   ├── InvalidExpressionFormatException.cs # Malformed expressions
│       │   └── UnsupportedExpressionException.cs   # Unsupported LINQ operations
│       │
│       ├── Executors/                    # Query executors
│       │   ├── ContentQueryExecutor.cs   # Content item query executor
│       │   ├── PageContentQueryExecutor.cs # Page content query executor
│       │   └── ReusableSchemaExecutor.cs   # Reusable schema query executor
│       │
│       ├── Expressions/                  # Expression processing pipeline
│       │   ├── Processors/               # Expression processors (Strategy pattern)
│       │   │   ├── BinaryExpressionProcessor.cs      # Binary operations (==, !=, <, >, etc.)
│       │   │   ├── ComparisonExpressionProcessor.cs  # Comparison operations
│       │   │   ├── ConditionalExpressionProcessor.cs # Ternary conditionals
│       │   │   ├── EnhancedCollectionProcessor.cs    # Collection operations (Any, Contains, All)
│       │   │   ├── EnhancedStringProcessor.cs        # String operations (Contains, StartsWith, etc.)
│       │   │   ├── EqualityExpressionProcessor.cs    # Equality operations
│       │   │   ├── LogicalExpressionProcessor.cs     # AND/OR logic
│       │   │   ├── MethodCallExpressionProcessor.cs  # Method calls
│       │   │   ├── NegatedExpressionProcessor.cs     # NOT operations
│       │   │   ├── NullCoalescingExpressionProcessor.cs # Null coalescing (??)
│       │   │   ├── RangeOptimizationProcessor.cs     # Range optimization (x > 5 && x < 10)
│       │   │   └── UnaryExpressionProcessor.cs       # Unary operations
│       │   └── Visitors/                 # Expression visitors (Visitor pattern)
│       │       └── ContentItemQueryExpressionVisitor.cs # Main expression tree visitor
│       │
│       ├── Extensions/                   # Extension methods
│       │   ├── DebuggingExtensions.cs    # Debugging utilities
│       │   ├── ExpressionExtensions.cs   # Expression utilities
│       │   ├── LoggingExtensions.cs      # Logging utilities
│       │   └── TypeExtensions.cs         # Type utilities
│       │
│       ├── Properties/                   # Assembly metadata
│       │   └── AssemblyInfo.cs           # Assembly attributes
│       │
│       ├── bin/                          # Build output (excluded from source control)
│       │   └── Debug/                    # Debug build artifacts
│       │       ├── net8.0/               # .NET 8 build
│       │       └── net9.0/               # .NET 9 build
│       │
│       ├── obj/                          # Intermediate build files (excluded from source control)
│       │   ├── project.assets.json       # NuGet package references
│       │   └── ...                       # Other build artifacts
│       │
│       ├── DependencyInjection.cs        # Service registration extension methods
│       ├── packages.lock.json            # NuGet package lock file (for reproducible builds)
│       └── XperienceCommunity.DataContext.csproj # Project file
│
├── tests/                                # Test projects
│   ├── XperienceCommunity.DataContext.Tests/
│   │   ├── ProcessorTests/               # Expression processor tests
│   │   │   ├── BinaryExpressionProcessorTests.cs
│   │   │   ├── ComparisonExpressionProcessorTests.cs
│   │   │   ├── EnhancedCollectionProcessorTests.cs
│   │   │   ├── EnhancedMethodCallExpressionTests.cs
│   │   │   ├── EnhancedStringProcessorTests.cs
│   │   │   ├── EqualityExpressionProcessorTests.cs
│   │   │   ├── LogicalExpressionProcessorIntegrationTests.cs
│   │   │   ├── LogicalExpressionProcessorTests.cs
│   │   │   ├── MethodCallExpressionProcessorEdgeCasesTests.cs
│   │   │   ├── MethodCallExpressionProcessorTests.cs
│   │   │   ├── NegatedExpressionProcessorTests.cs
│   │   │   ├── RangeOptimizationProcessorTests.cs
│   │   │   └── UnaryExpressionProcessorTests.cs
│   │   │
│   │   ├── bin/                          # Test build output
│   │   │   └── Debug/
│   │   │       └── net9.0/               # Test target framework
│   │   │
│   │   ├── obj/                          # Test intermediate files
│   │   │
│   │   ├── ContentItemContextTests.cs    # Content context tests
│   │   ├── ContentItemQueryExpressionVisitorTests.cs # Visitor tests
│   │   ├── ContentQueryExecutorTests.cs  # Executor tests
│   │   ├── ExpressionContextTests.cs     # Expression context tests
│   │   ├── PageContentQueryExecutorTests.cs # Page executor tests
│   │   ├── ProcessorSupportedQueryExecutorTests.cs # Processor executor tests
│   │   ├── ReusableSchemaExecutorTests.cs # Schema executor tests
│   │   ├── XperienceContextBuilderTests.cs # Builder tests
│   │   ├── XperienceDataContextTests.cs  # Factory tests
│   │   ├── packages.lock.json            # Test package lock file
│   │   └── XperienceCommunity.DataContext.Tests.csproj # Test project file
│   │
│   ├── Directory.Build.props             # Shared test project properties
│   └── Directory.Packages.props          # Test package versions (Central Package Management)
│
├── TestResults/                          # Test execution results (excluded from source control)
│   ├── test_results_net9.trx             # .NET 9 test results
│   └── test_results.trx                  # Test results
│
├── Directory.Build.props                 # Shared build properties (all projects)
├── Directory.Build.targets               # Shared build targets (all projects)
├── Directory.Packages.props              # Centralized package versions (CPM)
├── LICENSE                               # MIT License
├── nuget.config                          # NuGet configuration
├── README.md                             # Project README
├── xperience-community-data-context.code-workspace # VS Code workspace
└── XperienceCommunity.DataContext.sln    # Solution file

```

---

## Key Directory Annotations

### `.github/`
- **Purpose**: GitHub-specific configurations including workflows, Copilot instructions, and documentation
- **CI/CD Workflows**: Automated build, test, and release pipelines
- **Instructions**: AI-assisted development documentation (this directory)

### `docs/`
- **Purpose**: User-facing documentation and guides
- **Key Files**: Debugging guide, product documentation, architecture overview

### `src/XperienceCommunity.DataContext/`
- **Purpose**: Main library source code
- **Architecture**: Organized by layer (Abstractions, Core, Contexts, Expressions, etc.)
- **Key Pattern**: Clear separation between interfaces (Abstractions/), implementations (Contexts/), and base classes (Core/)

### `src/.../Abstractions/`
- **Purpose**: Interfaces and contracts
- **Pattern**: All public APIs are defined here first as interfaces
- **Benefit**: Enables dependency injection, mocking, and testing

### `src/.../Core/`
- **Purpose**: Base classes that provide common functionality
- **Impact**: `BaseDataContext<T, TExecutor>` reduced code duplication by 70%+
- **Pattern**: Template Method pattern for extensibility

### `src/.../Expressions/`
- **Purpose**: Expression tree processing and translation
- **Pattern**: Visitor Pattern + Strategy Pattern
- **Components**:
  - **Visitors/**: Walk expression trees
  - **Processors/**: Handle specific expression types (12 processors)

### `src/.../Expressions/Processors/`
- **Purpose**: Strategy implementations for different expression types
- **Count**: 12 specialized processors
- **Extensibility**: Add new expression support by creating new processor

### `tests/`
- **Purpose**: Unit and integration tests
- **Framework**: xUnit with NSubstitute for mocking
- **Coverage**: Expression processors, contexts, executors
- **Target**: .NET 9 (single target for tests)

### `tests/.../ProcessorTests/`
- **Purpose**: Focused tests for expression processors
- **Pattern**: One test file per processor
- **Coverage**: Edge cases, integration scenarios, performance

---

## File Naming Conventions

### Source Files
- **Classes**: `ClassName.cs` (e.g., `ContentItemContext.cs`)
- **Interfaces**: `IInterfaceName.cs` (e.g., `IDataContext.cs`)
- **Extension Methods**: `FeatureExtensions.cs` (e.g., `DebuggingExtensions.cs`)

### Test Files
- **Unit Tests**: `ClassNameTests.cs` (e.g., `ContentItemContextTests.cs`)
- **Integration Tests**: `FeatureIntegrationTests.cs`
- **Processor Tests**: Located in `ProcessorTests/` subdirectory

---

## Folder Organization Principles

1. **Namespace Mirrors Folder Structure**: `XperienceCommunity.DataContext.Expressions.Processors` → `src/.../Expressions/Processors/`
2. **Separation of Concerns**: Each folder has a clear, single responsibility
3. **Layer Segregation**: Abstractions, Core, Implementations are separate
4. **Test Isolation**: Tests mirror source structure with `Tests` suffix
5. **Configuration Centralization**: `Directory.Build.props` and `Directory.Packages.props` at solution root

---

## Build Artifacts & Ignored Files

### Excluded from Source Control (.gitignore)
- `bin/` - Build output
- `obj/` - Intermediate build files
- `TestResults/` - Test execution results
- `*.user` - User-specific settings
- `*.suo` - Visual Studio solution user options

### Included in Source Control
- `packages.lock.json` - NuGet package lock files (for reproducible builds)
- `.github/workflows/*.yml` - CI/CD workflows
- `Directory.*.props` - Shared build properties

---

## Navigation Tips

### Finding Features
1. **Interfaces**: Start in `Abstractions/` to understand contracts
2. **Implementation**: Look in `Contexts/` for concrete implementations
3. **Base Logic**: Check `Core/` for shared functionality
4. **Expression Support**: Browse `Expressions/Processors/` for LINQ operation support
5. **Configuration**: Check `Configurations/` for setup and DI registration

### Adding New Features
1. **New Context Type**: Add interface in `Abstractions/`, implementation in `Contexts/`, executor in `Executors/`
2. **New Expression**: Add processor in `Expressions/Processors/`, register in `ContentItemQueryExpressionVisitor`
3. **New Extension**: Add in appropriate `Extensions/` file or create new if unrelated

---

## Project File Organization

### Multi-Targeting
```xml
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```
- Source projects target both .NET 8 and .NET 9
- Test projects target .NET 9 only (for simplicity)

### Central Package Management (CPM)
```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="Kentico.Xperience.Core" Version="[30.3.1,)" />
```
- All package versions defined in `Directory.Packages.props`
- Projects reference packages without versions
- Ensures version consistency across solution

---

## Recommended VS Code Workspace Settings

```json
{
  "folders": [
    { "path": "." }
  ],
  "settings": {
    "files.exclude": {
      "**/bin": true,
      "**/obj": true
    },
    "search.exclude": {
      "**/bin": true,
      "**/obj": true,
      "**/packages.lock.json": true
    }
  }
}
```

---

**Last Updated**: December 28, 2025
**Folder Count**: 40+ directories
**File Count**: 100+ source/test files
