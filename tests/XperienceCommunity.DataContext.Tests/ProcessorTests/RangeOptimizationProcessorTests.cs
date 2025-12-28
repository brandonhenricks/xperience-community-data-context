using System.Linq.Expressions;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class RangeOptimizationProcessorTests
{
    [Fact]
    public void CanProcess_ShouldReturnTrue_ForAndBinaryExpression()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new RangeOptimizationProcessor(context, visitor);

        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Value));
        var left = Expression.GreaterThanOrEqual(member, Expression.Constant(10));
        var right = Expression.LessThanOrEqual(member, Expression.Constant(20));
        var andExpression = Expression.AndAlso(left, right);

        // Act
        var result = processor.CanProcess(andExpression);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForOrBinaryExpression()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new RangeOptimizationProcessor(context, visitor);

        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Value));
        var left = Expression.GreaterThanOrEqual(member, Expression.Constant(10));
        var right = Expression.LessThanOrEqual(member, Expression.Constant(20));
        var orExpression = Expression.OrElse(left, right);

        // Act
        var result = processor.CanProcess(orExpression);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Process_ShouldHandleSimpleRange()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new RangeOptimizationProcessor(context, visitor);

        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Value));
        var left = Expression.GreaterThanOrEqual(member, Expression.Constant(10));
        var right = Expression.LessThanOrEqual(member, Expression.Constant(20));
        var andExpression = Expression.AndAlso(left, right);

        // Act
        processor.Process(andExpression);

        // Assert
        context.Received().AddParameter(Arg.Any<string>(), 10);
        context.Received().AddParameter(Arg.Any<string>(), 20);
        context.Received().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldHandleNonRangeExpression()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new RangeOptimizationProcessor(context, visitor);

        var param = Expression.Parameter(typeof(TestClass), "x");
        var left = Expression.Property(param, nameof(TestClass.IsActive));
        var right = Expression.Property(param, nameof(TestClass.IsActive));
        var andExpression = Expression.AndAlso(left, right);

        // Act
        processor.Process(andExpression);

        // Assert
        context.DidNotReceive().AddParameter(Arg.Any<string>(), Arg.Any<object>());
        context.DidNotReceive().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    private class TestClass
    {
        public int Value { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
