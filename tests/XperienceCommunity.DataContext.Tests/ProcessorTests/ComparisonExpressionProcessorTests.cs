using System.Linq.Expressions;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class ComparisonExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new ComparisonExpressionProcessor(context, true, false);
        Assert.NotNull(processor);
    }

    [Fact]
    public void CanProcess_ReturnsTrue_WhenMemberAndConstant()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new ComparisonExpressionProcessor(context, true, false);

        var member = Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.Value));
        var constant = Expression.Constant(5);
        var node = Expression.MakeBinary(ExpressionType.Equal, member, constant);

        Assert.True(processor.CanProcess(node));
    }

    [Fact]
    public void CanProcess_ReturnsFalse_WhenNotMemberAndConstant()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new ComparisonExpressionProcessor(context, true, false);

        var left = Expression.Constant(1);
        var right = Expression.Constant(2);
        var node = Expression.MakeBinary(ExpressionType.Equal, left, right);

        Assert.False(processor.CanProcess(node));
    }

    [Fact]
    public void Process_Throws_WhenNoMemberOrConstant()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new ComparisonExpressionProcessor(context, true, false);

        var left = Expression.Parameter(typeof(TestClass), "x");
        var right = Expression.Parameter(typeof(TestClass), "y");
        var node = Expression.MakeBinary(ExpressionType.Equal, left, right);

        Assert.Throws<InvalidOperationException>(() => processor.Process((BinaryExpression)node));
    }

    private class TestClass
    {
        public int Value { get; set; }
    }
}
