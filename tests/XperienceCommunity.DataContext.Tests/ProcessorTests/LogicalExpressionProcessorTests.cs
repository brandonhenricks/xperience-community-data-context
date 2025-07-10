using System.Linq.Expressions;
using CMS.ContentEngine;
using NSubstitute;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class LogicalExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);
        Assert.NotNull(processor);
    }

    [Fact]
    public void Constructor_WithVisitFunction_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        Expression visitFunction(Expression expr) => expr;
        var processor = new Processors.LogicalExpressionProcessor(context, true, visitFunction);
        Assert.NotNull(processor);
    }

    [Theory]
    [InlineData(true, ExpressionType.AndAlso, true)]
    [InlineData(false, ExpressionType.OrElse, true)]
    [InlineData(true, ExpressionType.OrElse, false)]
    [InlineData(false, ExpressionType.AndAlso, false)]
    public void CanProcess_ShouldReturnExpectedResult_ForLogicalExpressions(bool isAnd, ExpressionType nodeType, bool expected)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, isAnd);

        var left = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.MakeBinary(nodeType, left, right);

        var result = processor.CanProcess(binary);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonBinaryExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        var constant = Expression.Constant(true);

        var result = processor.CanProcess(constant);

        Assert.False(result);
    }

    [Theory]
    [InlineData(true, "AND")]
    [InlineData(false, "OR")]
    public void Process_ShouldPushLogicalGrouping_AndAddWhereAction(bool isAnd, string expectedGrouping)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, isAnd);

        var left = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.MakeBinary(isAnd ? ExpressionType.AndAlso : ExpressionType.OrElse, left, right);

        processor.Process(binary);

        context.Received(1).PushLogicalGrouping(expectedGrouping);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldHandleBooleanConstants_InAndExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        var left = Expression.Constant(true);
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var binary = Expression.AndAlso(left, right);

        processor.Process(binary);

        // Should process both operands
        context.Received().PushLogicalGrouping("AND");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldHandleBooleanConstants_InOrExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, false);

        var left = Expression.Constant(false);
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var binary = Expression.OrElse(left, right);

        processor.Process(binary);

        context.Received().PushLogicalGrouping("OR");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldHandleBooleanMemberExpressions()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        var left = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.AndAlso(left, right);

        processor.Process(binary);

        context.Received().PushLogicalGrouping("AND");
        context.Received(2).AddParameter("IsActive", true);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldThrowException_ForNonBooleanMemberExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        // Use a binary expression (Equal) as left operand, which is not supported without a visit function
        var left = Expression.Equal(
            Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.Name)),
            Expression.Constant(null, typeof(string))
        );
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.AndAlso(left, right);

        Assert.Throws<UnsupportedExpressionException>(() => processor.Process(binary));
    }

    [Fact]
    public void Process_ShouldUseVisitFunction_ForComplexExpressions()
    {
        var context = Substitute.For<IExpressionContext>();
        var visitCalled = false;
        Expression visitFunction(Expression expr)
        {
            visitCalled = true;
            return expr;
        }

        var processor = new Processors.LogicalExpressionProcessor(context, true, visitFunction);

        var left = Expression.Equal(
            Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.Name)),
            Expression.Constant("test"));
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.AndAlso(left, right);

        processor.Process(binary);

        Assert.True(visitCalled);
        context.Received().PushLogicalGrouping("AND");
    }

    [Fact]
    public void Process_ShouldThrowUnsupportedException_ForUnsupportedExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        // Use a visit function that throws
        Expression visit(Expression expr) => throw new UnsupportedExpressionException("Test");
        var processor = new Processors.LogicalExpressionProcessor(context, true, visit);

        // Use a binary expression as operand to force visit function to be called
        var left = Expression.Equal(
            Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.Name)),
            Expression.Constant("test")
        );
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.AndAlso(left, right);

        Assert.Throws<UnsupportedExpressionException>(() => processor.Process(binary));
    }

    [Fact]
    public void Process_ShouldThrowUnsupportedException_WhenCannotProcess()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true); // AND processor

        var left = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.IsActive));
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.OrElse(left, right); // OR expression to AND processor

        Assert.Throws<UnsupportedExpressionException>(() => processor.Process(binary));
    }

    [Theory]
    [InlineData(true, false)] // true && false => process false constant
    [InlineData(false, true)] // false && true => process false constant  
    public void Process_ShouldOptimizeBooleanConstants_InAndExpression(bool leftValue, bool rightValue)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        var left = Expression.Constant(leftValue);
        var right = Expression.Constant(rightValue);
        var binary = Expression.AndAlso(left, right);

        processor.Process(binary);

        context.Received().PushLogicalGrouping("AND");
        // Should have appropriate where actions for boolean constants
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Theory]
    [InlineData(true, false)] // true || false => process true constant
    [InlineData(false, true)] // false || true => process true constant
    public void Process_ShouldOptimizeBooleanConstants_InOrExpression(bool leftValue, bool rightValue)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, false);

        var left = Expression.Constant(leftValue);
        var right = Expression.Constant(rightValue);
        var binary = Expression.OrElse(left, right);

        processor.Process(binary);

        context.Received().PushLogicalGrouping("OR");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void CanProcess_ShouldValidateProcessableExpressions()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        // Use two boolean constants, which are valid and processable
        var left = Expression.Constant(true);
        var right = Expression.Constant(false);
        var binary = Expression.AndAlso(left, right);

        var result = processor.CanProcess(binary);

        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForMixedProcessableExpressions()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);

        var left = Expression.Constant(true); // Processable boolean constant
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive)); // Processable boolean property
        var binary = Expression.AndAlso(left, right);

        var result = processor.CanProcess(binary);

        Assert.True(result); // Should return true because at least one operand is processable
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Age { get; set; }
    }
}
