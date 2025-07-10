using System.Linq.Expressions;
using CMS.ContentEngine;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

/// <summary>
/// Integration tests to verify LogicalExpressionProcessor works correctly with complex expression scenarios
/// </summary>
public class LogicalExpressionProcessorIntegrationTests
{
    [Fact]
    public void ComplexLogicalExpression_ShouldProcessCorrectly()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitCallCount = 0;

        Expression visitFunction(Expression expr)
        {
            visitCallCount++;
            return expr;
        }

        var processor = new LogicalExpressionProcessor(context, true, visitFunction);

        // Create a complex expression: (x.Name == "test") && (x.Age > 18)
        var param = Expression.Parameter(typeof(TestClass), "x");
        var nameProperty = Expression.Property(param, nameof(TestClass.Name));
        var ageProperty = Expression.Property(param, nameof(TestClass.Age));

        var nameComparison = Expression.Equal(nameProperty, Expression.Constant("test"));
        var ageComparison = Expression.GreaterThan(ageProperty, Expression.Constant(18));

        var complexExpression = Expression.AndAlso(nameComparison, ageComparison);

        // Act
        processor.Process(complexExpression);

        // Assert
        context.Received(1).PushLogicalGrouping("AND");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
        Assert.Equal(2, visitCallCount); // Should visit both sub-expressions
    }

    [Fact]
    public void NestedLogicalExpression_ShouldProcessCorrectly()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitCallCount = 0;

        Expression visitFunction(Expression expr)
        {
            visitCallCount++;
            return expr;
        }

        var processor = new LogicalExpressionProcessor(context, false, visitFunction);

        // Create nested expression: (x.IsActive && x.IsVerified) || x.IsAdmin
        var param = Expression.Parameter(typeof(TestClass), "x");
        var isActiveProperty = Expression.Property(param, nameof(TestClass.IsActive));
        var isVerifiedProperty = Expression.Property(param, nameof(TestClass.IsVerified));
        var isAdminProperty = Expression.Property(param, nameof(TestClass.IsAdmin));

        var nestedAnd = Expression.AndAlso(isActiveProperty, isVerifiedProperty);
        var outerOr = Expression.OrElse(nestedAnd, isAdminProperty);

        // Act
        processor.Process(outerOr);

        // Assert
        context.Received(1).PushLogicalGrouping("OR");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
        // Should visit nested expression and boolean member
        Assert.True(visitCallCount >= 1);
    }

    [Fact]
    public void BooleanConstantOptimization_AndExpression_ShouldWork()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new LogicalExpressionProcessor(context, true);

        // Create expression: false && x.IsActive (should optimize to always false)
        var falseConstant = Expression.Constant(false);
        var isActiveProperty = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var andExpression = Expression.AndAlso(falseConstant, isActiveProperty);

        // Act
        processor.Process(andExpression);

        // Assert
        context.Received(1).PushLogicalGrouping("AND");
        context.Received().AddParameter("IsActive", true);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void BooleanConstantOptimization_OrExpression_ShouldWork()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new LogicalExpressionProcessor(context, false);

        // Create expression: true || x.IsActive (should optimize to always true)
        var trueConstant = Expression.Constant(true);
        var isActiveProperty = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var orExpression = Expression.OrElse(trueConstant, isActiveProperty);

        // Act
        processor.Process(orExpression);

        // Assert
        context.Received(1).PushLogicalGrouping("OR");
        context.Received().AddParameter("IsActive", true);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void MixedExpressionTypes_ShouldHandleGracefully()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitCallCount = 0;

        Expression visitFunction(Expression expr)
        {
            visitCallCount++;
            // Simulate processing by the visitor
            return expr;
        }

        var processor = new LogicalExpressionProcessor(context, true, visitFunction);

        // Create expression: x.IsActive && x.Name.Contains("test")
        var param = Expression.Parameter(typeof(TestClass), "x");
        var isActiveProperty = Expression.Property(param, nameof(TestClass.IsActive));
        var nameProperty = Expression.Property(param, nameof(TestClass.Name));
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var containsCall = Expression.Call(nameProperty, containsMethod, Expression.Constant("test"));

        var mixedExpression = Expression.AndAlso(isActiveProperty, containsCall);

        // Act
        processor.Process(mixedExpression);

        // Assert
        context.Received(1).PushLogicalGrouping("AND");
        context.Received(1).AddParameter("IsActive", true);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
        Assert.Equal(1, visitCallCount); // Should visit the method call expression
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public bool IsAdmin { get; set; }
        public int Age { get; set; }
    }
}
