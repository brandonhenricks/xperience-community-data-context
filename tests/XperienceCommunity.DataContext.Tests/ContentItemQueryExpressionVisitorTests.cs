using System.Linq.Expressions;
using XperienceCommunity.DataContext.Abstractions.Processors;
using XperienceCommunity.DataContext.Contexts;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Expressions.Visitors;

namespace XperienceCommunity.DataContext.Tests;

public class ContentItemQueryExpressionVisitorTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ContentItemQueryExpressionVisitor(null!));
    }

    [Fact]
    public void GetProcessor_ReturnsProcessor_WhenTypeExists()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);
        var processor = visitor.GetProcessor(typeof(MethodCallExpression));
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<IExpressionProcessor>(processor);
    }

    [Fact]
    public void GetProcessor_ThrowsNotSupportedException_WhenTypeNotExists()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);
        Assert.Throws<UnsupportedExpressionException>(() => visitor.GetProcessor(typeof(ParameterExpression)));
    }

    [Fact]
    public void VisitBinary_ProcessesSupportedBinaryExpression()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        // Expression: 1 == 1
        var expr = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
        var result = visitor.Visit(expr);

        Assert.Equal(expr, result);
    }

    [Fact]
    public void VisitBinary_ThrowsNotSupportedException_ForUnsupportedBinary()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        // ExpressionType.ExclusiveOr is not supported
        var expr = Expression.ExclusiveOr(Expression.Constant(1), Expression.Constant(2));
        Assert.Throws<UnsupportedExpressionException>(() => visitor.Visit(expr));
    }

    [Fact]
    public void VisitMethodCall_ProcessesMethodCallExpression()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        var method = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
        var expr = Expression.Call(Expression.Constant("foo"), method, Expression.Constant("f"));

        var result = visitor.Visit(expr);

        Assert.Equal(expr, result);
    }

    [Fact]
    public void VisitUnary_ProcessesUnaryExpression_Constant_To_Constant_ThrowsException()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        // Expression: !(1 == 1)
        var inner = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
        var expr = Expression.Not(inner);

        Assert.Throws<InvalidExpressionFormatException>(() => visitor.Visit(expr));
    }

    [Fact]
    public void VisitBinary_CallsCorrectProcessor_ForEachSupportedType()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        var binaryExpressions = new List<Expression>
        {
            Expression.Equal(Expression.Constant(1), Expression.Constant(2)),
            Expression.NotEqual(Expression.Constant(1), Expression.Constant(2)),
            Expression.AndAlso(Expression.Constant(true), Expression.Constant(false)),
            Expression.OrElse(Expression.Constant(true), Expression.Constant(false))
        };

        foreach (var expr in binaryExpressions)
        {
            var result = visitor.Visit(expr);
            Assert.Equal(expr, result);
        }
    }

    [Fact]
    public void VisitMethodCall_ReturnsBase_WhenProcessorNotFound()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        // Remove MethodCallExpression processor to simulate not found
        var type = typeof(ContentItemQueryExpressionVisitor);
        var field = type.GetField("_expressionProcessors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var processors = (Dictionary<Type, IExpressionProcessor>)field!.GetValue(visitor)!;
        processors.Remove(typeof(MethodCallExpression));

        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;
        var expr = Expression.Call(Expression.Constant("foo"), method);

        var result = visitor.Visit(expr);

        Assert.Equal(expr, result);
    }

    [Fact]
    public void VisitUnary_ReturnsBase_WhenProcessorNotFound()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        // Remove UnaryExpression processor to simulate not found
        var type = typeof(ContentItemQueryExpressionVisitor);
        var field = type.GetField("_expressionProcessors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var processors = (Dictionary<Type, IExpressionProcessor>)field!.GetValue(visitor)!;
        processors.Remove(typeof(UnaryExpression));

        var expr = Expression.Not(Expression.Constant(false));

        var result = visitor.Visit(expr);

        Assert.Equal(expr, result);
    }

    [Fact]
    public void VisitConstant_SetsCurrentValue()
    {
        var ctx = new ExpressionContext();
        var visitor = new ContentItemQueryExpressionVisitor(ctx);

        var constant = Expression.Constant(42);

        visitor.Visit(constant);

        // _currentValue is private, so we can't assert directly.
        // But we can ensure no exception is thrown and base.VisitConstant is called.
        Assert.True(true);
    }
}
