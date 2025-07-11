using System.Linq.Expressions;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class NegatedExpressionProcessorTests
{
    [Fact]
    public void CanProcess_ShouldReturnTrue_ForUnaryNotExpression()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new NegatedExpressionProcessor(context, visitor);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.IsActive));
        var notExpression = Expression.Not(member);

        // Act
        var result = processor.CanProcess(notExpression);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonUnaryNotExpression()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new NegatedExpressionProcessor(context, visitor);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.IsActive));

        // Act
        var result = processor.CanProcess(member);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Process_ShouldHandleNegatedBooleanMember()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new NegatedExpressionProcessor(context, visitor);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.IsActive));
        var notExpression = Expression.Not(member);

        // Act
        processor.Process(notExpression);

        // Assert
        context.Received().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldHandleNegatedStringMethod()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var visitor = Substitute.For<Func<Expression, Expression>>();
        var processor = new NegatedExpressionProcessor(context, visitor);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Name));
        var containsMethod = Expression.Call(member, nameof(string.Contains), null, Expression.Constant("test"));
        var notExpression = Expression.Not(containsMethod);

        // Act
        processor.Process(notExpression);

        // Assert
        context.Received().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    private class TestClass
    {
        public bool IsActive { get; set; }
        public string Name { get; set; } = "";
    }
}
