# Debugging and Diagnostics Guide

This guide provides comprehensive information about the debugging and diagnostic features available in XperienceCommunity.DataContext to help developers troubleshoot issues and gain insights into query execution.

## Overview

The XperienceCommunity.DataContext library includes extensive debugging support through:

1. **Debugger Display Attributes** - Enhanced visualization in the debugger
2. **Diagnostic Logging** - Detailed execution tracking and performance metrics
3. **Performance Counters** - Query execution statistics
4. **Extension Methods** - Easy-to-use debugging utilities
5. **Telemetry Integration** - OpenTelemetry/Activity support

## Debugger Display Attributes

### ExpressionContext Debugging

The `ExpressionContext` class includes comprehensive debugging support:

```csharp
// When debugging, you'll see:
// Parameters: 3, Members: User.Name, WhereActions: 2, Groupings: 1

var context = new ExpressionContext();
context.AddParameter("name", "John");
context.PushMember("User");
context.PushMember("Name");
context.PushLogicalGrouping("AND");
```

**Debugger Features:**

- **Parameters Count**: Number of query parameters
- **Current Member Path**: Dot-separated property access chain (e.g., "User.Name.FirstName")
- **Where Actions Count**: Number of where clause fragments
- **Logical Groupings Count**: Depth of AND/OR nesting

### BaseDataContext Debugging

All data context classes show rich debugging information:

```csharp
// Debugger display shows:
// ContentType: BlogPost, Language: en-US, Parameters: 2, HasQuery: true, CacheTimeout: 60min

var context = dataContext.ForContentType<BlogPost>()
    .InLanguage("en-US")
    .Where(x => x.Title.Contains("Tutorial"));
```

### Expression Processor Debugging

Individual processors show their state and context:

```csharp
// BinaryExpressionProcessor debugger display:
// Processor: Binary, Context: 2 params, 1 actions
```

## Diagnostic Logging

### Enabling Diagnostics

```csharp
// Enable diagnostics globally
DataContextDiagnostics.DiagnosticsEnabled = true;
DataContextDiagnostics.TraceLevel = LogLevel.Debug;

// Or enable for a specific context using extension methods
var context = dataContext.ForContentType<BlogPost>()
    .EnableDiagnostics(LogLevel.Debug)
    .Where(x => x.IsPublished);
```

### Built-in Diagnostic Categories

The library automatically logs diagnostics in several categories:

- **ExpressionProcessing**: Expression tree traversal and translation
- **QueryExecution**: Query building and execution timing
- **ContextConfiguration**: Data context setup and configuration
- **QueryTiming**: Detailed timing information

### Getting Diagnostic Reports

```csharp
// Get a full diagnostic report
string report = DataContextDiagnostics.GetDiagnosticReport();
Console.WriteLine(report);

// Filter by category
string queryReport = DataContextDiagnostics.GetDiagnosticReport("QueryExecution");

// Get from context instance
var context = dataContext.ForContentType<BlogPost>();
string contextReport = context.GetDiagnosticReport("ExpressionProcessing");
```

### Performance Statistics

```csharp
// Get performance metrics
var stats = DataContextDiagnostics.GetPerformanceStats();
Console.WriteLine($"Total Queries: {stats["TotalQueries"]}");
Console.WriteLine($"Queries Last 5 Minutes: {stats["QueriesLast5Minutes"]}");
Console.WriteLine($"Error Count: {stats["ErrorCount"]}");

// Or from context
var contextStats = context.GetPerformanceStats();
```

## Debugging Extension Methods

### ExecuteWithDiagnostics

Wrap your queries with detailed timing information:

```csharp
var results = await context.ExecuteWithDiagnostics(
    "GetPublishedBlogPosts",
    async ctx => await ctx.Where(x => x.IsPublished).ToListAsync()
);

// This automatically logs:
// [QueryTiming] Starting GetPublishedBlogPosts for BlogPost
// [QueryTiming] Completed GetPublishedBlogPosts for BlogPost in 145ms
```

### Custom Diagnostic Logging

```csharp
var context = dataContext.ForContentType<BlogPost>()
    .LogDiagnostic("Starting complex query operation")
    .Where(x => x.Category == "Technology")
    .LogDiagnostic("Applied category filter", LogLevel.Debug)
    .Take(10);
```

### Debug String Representation

```csharp
var debugInfo = context.ToDebugString();
Console.WriteLine(debugInfo);

// Output:
// DataContext<BlogPost>
//   Type: ContentItemContext`1
//   Hash: A1B2C3D4
//   Diagnostics Enabled: True
//   Session Queries: 5
//   Session Errors: 0
```

## Performance Tracking

### Query Executor Performance Counters (DEBUG Builds Only)

The `QueryExecutorPerformanceTracker` class tracks performance metrics in DEBUG builds with zero runtime cost in Release builds:

```csharp
using XperienceCommunity.DataContext.Diagnostics;

// Access performance metrics for a specific executor type
var executorTypeName = typeof(ContentQueryExecutor<BlogPost>).FullName;
var metrics = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);

Console.WriteLine($"Total Executions: {metrics.TotalExecutions}");
Console.WriteLine($"Total Time: {metrics.TotalExecutionTimeMs}ms");
Console.WriteLine($"Average Time: {metrics.AverageExecutionTimeMs:F2}ms");

// Get all tracked executor types
var trackedTypes = QueryExecutorPerformanceTracker.GetTrackedExecutorTypes();
foreach (var type in trackedTypes)
{
    var typeMetrics = QueryExecutorPerformanceTracker.GetMetrics(type);
    Console.WriteLine($"{type}: {typeMetrics.AverageExecutionTimeMs:F2}ms avg");
}

// Clear metrics for a specific executor
QueryExecutorPerformanceTracker.Clear(executorTypeName);

// Clear all metrics
QueryExecutorPerformanceTracker.Clear();
```

**Note**: Performance tracking uses `[Conditional("DEBUG")]` attributes, which means:
- In DEBUG builds: Full tracking with negligible overhead
- In Release builds: All tracking code is removed by the compiler (zero cost)

This approach:
- ✅ Eliminates thread contention in production
- ✅ Removes false sharing risks
- ✅ Improves testability (no shared static state across tests)
- ✅ Keeps core executor logic focused on query execution

## Telemetry Integration

### OpenTelemetry/Activity Support

The library automatically creates Activity spans for query execution:

```csharp
// Activity spans are created with tags:
// - contentType: The content type being queried
// - processorCount: Number of processors applied
// - executionTimeMs: Query execution time
// - resultCount: Number of results returned
// - error: Boolean indicating if an error occurred
// - errorMessage: Error message if applicable
```

### Integration with Application Insights

```csharp
// Configure your telemetry to capture DataContext activities
services.AddApplicationInsightsTelemetry();

// The ActivitySource name is: "XperienceCommunity.Data.Context.QueryExecution"
```

## Logger Integration

### Connecting with Microsoft.Extensions.Logging

```csharp
// Attach data context diagnostics to your logger
logger.AttachDataContextDiagnostics(LogLevel.Information);

// Or configure in DI
services.AddLogging(builder => 
{
    builder.AddConsole();
    // DataContext will automatically use the configured logger
});
```

## Debugging Specific Scenarios

### Expression Processing Issues

When debugging expression translation problems:

```csharp
// Enable detailed expression processing diagnostics
DataContextDiagnostics.DiagnosticsEnabled = true;
DataContextDiagnostics.TraceLevel = LogLevel.Debug;

var query = dataContext.ForContentType<BlogPost>()
    .Where(x => x.Tags.Contains("tutorial") && x.IsPublished);

// Check the diagnostic report for expression processing details
var report = DataContextDiagnostics.GetDiagnosticReport("ExpressionProcessing");
```

### Performance Issues

For performance debugging:

```csharp
var context = dataContext.ForContentType<BlogPost>()
    .EnableDiagnostics(LogLevel.Information);

var results = await context.ExecuteWithDiagnostics(
    "SlowQuery", 
    async ctx => await ctx
        .Where(x => x.Content.Contains("performance"))
        .WithLinkedItems(3)
        .ToListAsync()
);

// Review timing information
var stats = context.GetPerformanceStats();
```

### Cache Behavior

To debug caching behavior:

```csharp
// The debugger display shows cache configuration
// CacheTimeout: 60min indicates cache is active

var context = dataContext.ForContentType<BlogPost>();
// Examine context in debugger to see cache settings
```

## Best Practices

### Development Environment

1. **Enable diagnostics during development**:

   ```csharp
   #if DEBUG
   DataContextDiagnostics.DiagnosticsEnabled = true;
   DataContextDiagnostics.TraceLevel = LogLevel.Debug;
   #endif
   ```

2. **Use conditional debugging**:

   ```csharp
   var context = dataContext.ForContentType<BlogPost>();
   
   #if DEBUG
   context = context.EnableDiagnostics();
   #endif
   ```

### Production Environment

1. **Use structured logging**:

   ```csharp
   // Let the built-in ILogger integration handle production logging
   services.AddXperienceDataContext(config => 
   {
       // Configuration
   });
   ```

2. **Monitor performance counters**:

   ```csharp
   // Periodically check performance metrics
   var stats = DataContextDiagnostics.GetPerformanceStats();
   if ((int)stats["ErrorCount"] > threshold)
   {
       // Alert or log
   }
   ```

### Memory Management

The diagnostic system automatically manages memory by:

- Keeping only the last 1000 diagnostic entries
- Automatically removing older entries when the limit is reached
- Using efficient data structures for tracking

## Troubleshooting Common Issues

### High Memory Usage

Check if diagnostics are accidentally enabled in production:

```csharp
if (DataContextDiagnostics.DiagnosticsEnabled)
{
    logger.LogWarning("DataContext diagnostics are enabled in production");
}
```

### Performance Degradation

Monitor average execution times (DEBUG builds only):

```csharp
using XperienceCommunity.DataContext.Diagnostics;

var executorTypeName = typeof(ContentQueryExecutor<T>).FullName;
var metrics = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);

if (metrics.AverageExecutionTimeMs > acceptableThreshold)
{
    // Investigate slow queries
    logger.LogWarning(
        "Executor {ExecutorType} has high average execution time: {AvgTime}ms",
        executorTypeName,
        metrics.AverageExecutionTimeMs);
}
```

### Expression Processing Errors

Enable detailed expression diagnostics:

```csharp
DataContextDiagnostics.DiagnosticsEnabled = true;
var report = DataContextDiagnostics.GetDiagnosticReport("ExpressionProcessing", LogLevel.Error);
```

This comprehensive debugging support ensures developers can easily troubleshoot issues, optimize performance, and gain deep insights into the library's operation.
