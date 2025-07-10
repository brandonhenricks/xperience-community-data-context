using CMS.ContentEngine;
using NSubstitute;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class BinaryExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();

        var processor = new Processors.BinaryExpressionProcessor(context);

        Assert.NotNull(processor);
    }
    [Fact]
    public void CanProcess_ShouldReturnTrue_ForBinaryExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);
        var expr = System.Linq.Expressions.Expression.Equal(
            System.Linq.Expressions.Expression.Constant(1),
            System.Linq.Expressions.Expression.Constant(1)
        );

        var result = processor.CanProcess(expr);

        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonBinaryExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);
        var expr = System.Linq.Expressions.Expression.Constant(1);

        var result = processor.CanProcess(expr);

        Assert.False(result);
    }

    [Theory]
    [InlineData(System.Linq.Expressions.ExpressionType.Equal, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.NotEqual, false)]
    public void Process_ShouldHandle_EqualityExpressions(System.Linq.Expressions.ExpressionType nodeType, bool isEqual)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var member = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Parameter(typeof(TestClass), "x"),
            typeof(TestClass).GetProperty(nameof(TestClass.Id))
        );
        var constant = System.Linq.Expressions.Expression.Constant(5);

        var expr = System.Linq.Expressions.Expression.MakeBinary(nodeType, member, constant);

        processor.Process((System.Linq.Expressions.BinaryExpression)expr);

        context.Received().AddParameter(nameof(TestClass.Id), 5);
        context.Received().AddWhereAction(Arg.Any<System.Action<WhereParameters>>());
    }

    [Theory]
    [InlineData(System.Linq.Expressions.ExpressionType.GreaterThan, true, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.GreaterThanOrEqual, true, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.LessThan, false, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.LessThanOrEqual, false, true)]
    public void Process_ShouldHandle_ComparisonExpressions(System.Linq.Expressions.ExpressionType nodeType, bool isGreaterThan, bool isEqual)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var member = System.Linq.Expressions.Expression.Property(
            System.Linq.Expressions.Expression.Parameter(typeof(TestClass), "x"),
            typeof(TestClass).GetProperty(nameof(TestClass.Id))
        );
        var constant = System.Linq.Expressions.Expression.Constant(10);

        var expr = System.Linq.Expressions.Expression.MakeBinary(nodeType, member, constant);

        processor.Process((System.Linq.Expressions.BinaryExpression)expr);

        context.Received().AddParameter(nameof(TestClass.Id), 10);
        context.Received().AddWhereAction(Arg.Any<System.Action<WhereParameters>>());
    }

    [Theory]
    [InlineData(System.Linq.Expressions.ExpressionType.AndAlso, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.OrElse, false)]
    [InlineData(System.Linq.Expressions.ExpressionType.And, true)]
    [InlineData(System.Linq.Expressions.ExpressionType.Or, false)]
    public void Process_ShouldHandle_LogicalExpressions(System.Linq.Expressions.ExpressionType nodeType, bool isAnd)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var left = System.Linq.Expressions.Expression.Constant(true);
        var right = System.Linq.Expressions.Expression.Constant(false);

        var expr = System.Linq.Expressions.Expression.MakeBinary(nodeType, left, right);

        processor.Process((System.Linq.Expressions.BinaryExpression)expr);

        context.Received().PushLogicalGrouping(isAnd ? "AND" : "OR");
        context.Received().AddWhereAction(Arg.Any<System.Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldThrow_ForUnsupportedExpressionType()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var left = System.Linq.Expressions.Expression.Constant(1);
        var right = System.Linq.Expressions.Expression.Constant(2);
        var expr = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Add, left, right);

        Assert.Throws<XperienceCommunity.DataContext.Exceptions.UnsupportedExpressionException>(() =>
            processor.Process((System.Linq.Expressions.BinaryExpression)expr));
    }

    [Fact]
    public void ProcessEquality_ShouldThrow_ForConstantToConstant()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var left = System.Linq.Expressions.Expression.Constant(1);
        var right = System.Linq.Expressions.Expression.Constant(2);
        var expr = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, left, right);

        Assert.Throws<System.InvalidOperationException>(() =>
            processor.Process((System.Linq.Expressions.BinaryExpression)expr));
    }

    [Fact]
    public void ProcessEquality_ShouldThrow_ForInvalidFormat()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var left = System.Linq.Expressions.Expression.Parameter(typeof(TestClass), "x");
        var right = System.Linq.Expressions.Expression.Parameter(typeof(TestClass), "y");
        var expr = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, left, right);

        Assert.Throws<XperienceCommunity.DataContext.Exceptions.InvalidExpressionFormatException>(() =>
            processor.Process((System.Linq.Expressions.BinaryExpression)expr));
    }

    [Fact]
    public void ProcessComparison_ShouldThrow_ForInvalidFormat()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.BinaryExpressionProcessor(context);

        var left = System.Linq.Expressions.Expression.Parameter(typeof(TestClass), "x");
        var right = System.Linq.Expressions.Expression.Parameter(typeof(TestClass), "y");
        var expr = System.Linq.Expressions.Expression.MakeBinary(
            System.Linq.Expressions.ExpressionType.GreaterThan,
            System.Linq.Expressions.Expression.Property(left, "Id"),
            System.Linq.Expressions.Expression.Property(right, "Id"));

        Assert.Throws<XperienceCommunity.DataContext.Exceptions.InvalidExpressionFormatException>(() =>
            processor.Process((System.Linq.Expressions.BinaryExpression)expr));
    }

    private class TestClass
    {
        public int Id { get; set; }
    }

}
