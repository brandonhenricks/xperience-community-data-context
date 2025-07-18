# Project Structure & Organization

## Solution Layout

```
├── src/XperienceCommunity.DataContext/     # Main library project
├── tests/XperienceCommunity.DataContext.Tests/  # Unit tests
├── docs/                                   # Documentation
├── images/                                 # Assets (logo, etc.)
└── Directory.Build.* files                 # Build configuration
```

## Library Architecture (`src/XperienceCommunity.DataContext/`)

### Core Folders

- **`Abstractions/`** - Interfaces and abstract base classes
- **`Contexts/`** - Main context implementations (ContentItem, PageContent, ReusableSchema, XperienceDataContext)
- **`Executors/`** - Query execution logic
- **`Expressions/`** - LINQ expression processing
- **`Configurations/`** - Configuration classes
- **`Extensions/`** - Extension methods
- **`Diagnostics/`** - Debugging and telemetry support
- **`Exceptions/`** - Custom exception types
- **`Core/`** - Base classes and core functionality

### Key Files

- **`DependencyInjection.cs`** - Service registration extensions
- **`XperienceCommunity.DataContext.csproj`** - Project configuration

## Architecture Patterns

### Three-Context Design

1. **ContentItemContext** - For content hub items (`IContentItemFieldsSource`)
2. **PageContentContext** - For web pages (`IWebPageFieldsSource`) 
3. **ReusableSchemaContext** - For flexible schema support (no constraints)

### Unified Access Pattern

- **XperienceDataContext** - Central factory for all context types
- Provides `ForContentType<T>()`, `ForPageContentType<T>()`, `ForReusableSchema<T>()` methods

### Base Class Hierarchy

- **BaseDataContext** - Common functionality for all contexts
- **ProcessorSupportedQueryExecutor** - Extensible query execution with processors

## Naming Conventions

- **Interfaces**: Prefix with `I` (e.g., `IContentItemContext<T>`)
- **Contexts**: Suffix with `Context` (e.g., `ContentItemContext<T>`)
- **Executors**: Suffix with `Executor` (e.g., `ContentQueryExecutor<T>`)
- **Processors**: Suffix with `Processor` (e.g., `IContentItemProcessor<T>`)
- **Extensions**: Suffix with `Extensions` for static extension classes

## File Organization Rules

- One primary class per file
- Group related interfaces in the same file when small
- Use folder structure to separate concerns (Abstractions, Implementations, etc.)
- Keep configuration classes in dedicated `Configurations/` folder
- Place extension methods in `Extensions/` folder