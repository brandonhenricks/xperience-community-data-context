using XperienceCommunity.DataContext.Diagnostics;

namespace XperienceCommunity.DataContext.Tests;

public class QueryExecutorPerformanceTrackerTests
{
    [Fact]
    public void RecordExecution_InDebugBuild_TracksMetrics()
    {
        // Arrange
        var executorTypeName = "TestExecutor1";
        QueryExecutorPerformanceTracker.Clear(executorTypeName);

        // Act - Record multiple executions
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 100);
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 200);
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 300);

        // Assert
        var metrics = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);

#if DEBUG
        // In DEBUG builds, metrics should be tracked
        Assert.Equal(3, metrics.TotalExecutions);
        Assert.Equal(600, metrics.TotalExecutionTimeMs);
        Assert.Equal(200.0, metrics.AverageExecutionTimeMs);
#else
        // In Release builds, performance tracking is disabled
        Assert.Equal(0, metrics.TotalExecutions);
        Assert.Equal(0, metrics.TotalExecutionTimeMs);
        Assert.Equal(0.0, metrics.AverageExecutionTimeMs);
#endif
    }

    [Fact]
    public void GetMetrics_ForNonExistentExecutor_ReturnsEmptyMetrics()
    {
        // Arrange
        var executorTypeName = "NonExistentExecutor";

        // Act
        var metrics = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalExecutions);
        Assert.Equal(0, metrics.TotalExecutionTimeMs);
        Assert.Equal(0.0, metrics.AverageExecutionTimeMs);
    }

    [Fact]
    public void Clear_RemovesSpecificExecutorMetrics()
    {
        // Arrange
        var executorTypeName = "TestExecutor2";
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 100);

        // Act
        QueryExecutorPerformanceTracker.Clear(executorTypeName);
        var metrics = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);

        // Assert
        Assert.Equal(0, metrics.TotalExecutions);
    }

    [Fact]
    public void Clear_RemovesAllMetrics()
    {
        // Arrange
        QueryExecutorPerformanceTracker.RecordExecution("Executor1", 100);
        QueryExecutorPerformanceTracker.RecordExecution("Executor2", 200);

#if DEBUG
        // Verify executors were tracked before clearing
        Assert.Equal(2, QueryExecutorPerformanceTracker.GetTrackedExecutorTypes().Count());
#endif

        // Act
        QueryExecutorPerformanceTracker.Clear();

        // Assert
        var trackedTypes = QueryExecutorPerformanceTracker.GetTrackedExecutorTypes().ToList();
#if DEBUG
        Assert.Empty(trackedTypes);
#else
        // In Release builds, tracking is disabled
        Assert.Empty(trackedTypes);
#endif
    }

    [Fact]
    public void GetTrackedExecutorTypes_ReturnsAllTrackedTypes()
    {
        // Arrange
        QueryExecutorPerformanceTracker.Clear();
        QueryExecutorPerformanceTracker.RecordExecution("Executor1", 100);
        QueryExecutorPerformanceTracker.RecordExecution("Executor2", 200);
        QueryExecutorPerformanceTracker.RecordExecution("Executor3", 300);

        // Act
        var trackedTypes = QueryExecutorPerformanceTracker.GetTrackedExecutorTypes().ToList();

        // Assert
#if DEBUG
        Assert.Contains("Executor1", trackedTypes);
        Assert.Contains("Executor2", trackedTypes);
        Assert.Contains("Executor3", trackedTypes);
#else
        // In Release builds, tracking is disabled
        Assert.Empty(trackedTypes);
#endif
    }

    [Fact]
    public void PerformanceMetrics_AverageCalculation_IsCorrect()
    {
        // Arrange

        // Simulate recording via the QueryExecutorPerformanceTracker
        var executorTypeName = "TestExecutor3";
        QueryExecutorPerformanceTracker.Clear(executorTypeName);

        // Act
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 50);
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 150);
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 200);
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 100);

        // Assert
        var result = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);
#if DEBUG
        Assert.Equal(4, result.TotalExecutions);
        Assert.Equal(500, result.TotalExecutionTimeMs);
        Assert.Equal(125.0, result.AverageExecutionTimeMs);
#else
        // In Release builds, performance tracking is disabled
        Assert.Equal(0, result.TotalExecutions);
#endif
    }

    [Fact]
    public void RecordExecution_WithZeroTime_IsTracked()
    {
        // Arrange
        var executorTypeName = "TestExecutorZero";
        QueryExecutorPerformanceTracker.Clear(executorTypeName);

        // Act
        QueryExecutorPerformanceTracker.RecordExecution(executorTypeName, 0);

        // Assert
        var metrics = QueryExecutorPerformanceTracker.GetMetrics(executorTypeName);
#if DEBUG
        Assert.Equal(1, metrics.TotalExecutions);
        Assert.Equal(0, metrics.TotalExecutionTimeMs);
        Assert.Equal(0.0, metrics.AverageExecutionTimeMs);
#else
        Assert.Equal(0, metrics.TotalExecutions);
#endif
    }
}
