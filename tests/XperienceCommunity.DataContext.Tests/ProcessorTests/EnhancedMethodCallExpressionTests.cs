using System;
using System.Linq.Expressions;
using Xunit;
using XperienceCommunity.DataContext.Contexts;
using XperienceCommunity.DataContext.Expressions.Visitors;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class EnhancedMethodCallExpressionTests
{
    [Fact]
    public void MethodCallExpressionProcessor_SupportsStringEndsWith()
    {
        // Arrange
        var context = new ExpressionContext();
        var processor = new MethodCallExpressionProcessor(context);

        var param = Expression.Parameter(typeof(string), "x");
        var constant = Expression.Constant("test");
        var endsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) });
        var call = Expression.Call(param, endsWithMethod!, constant);

        // Act & Assert
        Assert.True(processor.CanProcess(call));
        processor.Process(call);

        // Verify context was updated
        Assert.NotEmpty(context.WhereActions);
    }

    [Fact]
    public void MethodCallExpressionProcessor_SupportsEnumerableAny()
    {
        // Arrange
        var context = new ExpressionContext();
        var processor = new MethodCallExpressionProcessor(context);
        
        var list = Expression.Constant(new[] { 1, 2, 3 });
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Any" && m.GetParameters().Length == 1);
        var genericAnyMethod = anyMethod.MakeGenericMethod(typeof(int));
        var call = Expression.Call(null, genericAnyMethod, list);

        // Act & Assert
        Assert.True(processor.CanProcess(call));
        processor.Process(call);
        
        // Verify context was updated
        Assert.NotEmpty(context.WhereActions);
    }

    [Fact]
    public void NullCoalescingExpressionProcessor_SupportsBasicNullCoalescing()
    {
        // Arrange
        var context = new ExpressionContext();
        var processor = new NullCoalescingExpressionProcessor(context);

        var param = Expression.Parameter(typeof(string), "x");
        // Use a nullable reference type property for coalescing
        var member = param; // 'x' is of type string (nullable reference type)
        var constant = Expression.Constant("default");
        var coalesce = Expression.Coalesce(member, constant);

        // Act & Assert
        Assert.True(processor.CanProcess(coalesce));
        processor.Process(coalesce);

        // Verify context was updated
        Assert.NotEmpty(context.WhereActions);
    }

    [Fact]
    public void ContentItemQueryExpressionVisitor_SupportsCoalesceExpressions()
    {
        // Arrange
        var context = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(context);

        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.NullableString));
        var constant = Expression.Constant("default");
        var coalesce = Expression.Coalesce(member, constant);

        // Act
        var result = visitor.Visit(coalesce);

        // Assert
        Assert.Equal(coalesce, result);
        Assert.NotEmpty(context.WhereActions);
    }

    private class TestClass
    {
        public string? NullableString { get; set; }
    }
}
