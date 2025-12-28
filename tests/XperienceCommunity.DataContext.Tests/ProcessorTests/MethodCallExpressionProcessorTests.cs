using System.Linq.Expressions;
using CMS.ContentEngine;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class MethodCallExpressionProcessorTests
{
    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonMethodCallExpression()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var expr = Expression.Constant(5);
        Assert.False(processor.CanProcess(expr));
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForUnsupportedMethod()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var method = typeof(object).GetMethod(nameof(object.ToString))!;
        var expr = Expression.Call(Expression.Constant("abc"), method);
        Assert.False(processor.CanProcess(expr));
    }

    [Theory]
    [InlineData(nameof(string.Contains))]
    [InlineData(nameof(string.StartsWith))]
    [InlineData(nameof(string.EndsWith))]
    [InlineData(nameof(string.ToLower))]
    [InlineData(nameof(string.ToUpper))]
    [InlineData(nameof(string.Trim))]
    public void CanProcess_ShouldReturnTrue_ForSupportedStringMethods(string methodName)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var method = typeof(string).GetMethod(methodName, new[] { typeof(string) }) ??
                     typeof(string).GetMethod(methodName, Type.EmptyTypes);
        var instance = Expression.Constant("abc");
        var args = method!.GetParameters().Length == 0
            ? Array.Empty<Expression>()
            : new[] { Expression.Constant("a") };
        var expr = Expression.Call(instance, method, args);
        Assert.True(processor.CanProcess(expr));
    }

    [Fact]
    public void Process_ShouldThrowNotSupportedException_ForUnsupportedMethod()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var method = typeof(object).GetMethod(nameof(object.ToString))!;
        var expr = Expression.Call(Expression.Constant("abc"), method);
        Assert.Throws<NotSupportedException>(() => processor.Process(expr));
    }

    [Fact]
    public void ProcessStringContains_ShouldAddParameterAndWhereAction()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var member = Expression.Property(Expression.Parameter(typeof(string), "x"), "Length");
        var stringMember = Expression.Property(Expression.Parameter(typeof(TestClass), "t"), nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        var expr = Expression.Call(stringMember, method, Expression.Constant("foo"));
        processor.Process(expr as MethodCallExpression);
        context.Received().AddParameter(nameof(TestClass.Name), "foo");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void ProcessStringStartsWith_ShouldAddParameterAndWhereAction()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var stringMember = Expression.Property(Expression.Parameter(typeof(TestClass), "t"), nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
        var expr = Expression.Call(stringMember, method, Expression.Constant("bar"));
        processor.Process(expr as MethodCallExpression);
        context.Received().AddParameter(nameof(TestClass.Name), "bar");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void ProcessStringEndsWith_ShouldAddParameterAndWhereAction()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var stringMember = Expression.Property(Expression.Parameter(typeof(TestClass), "t"), nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
        var expr = Expression.Call(stringMember, method, Expression.Constant("baz"));
        processor.Process(expr as MethodCallExpression);
        context.Received().AddParameter(nameof(TestClass.Name), "baz");
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void ProcessStringIsNullOrEmpty_ShouldAddWhereAction()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var param = Expression.Property(Expression.Parameter(typeof(TestClass), "t"), nameof(TestClass.Name));
        var method = typeof(string).GetMethod("IsNullOrEmpty", new[] { typeof(string) })!;
        var expr = Expression.Call(method, param);
        processor.Process(expr as MethodCallExpression);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void ProcessEnumerableAny_ShouldAddWhereAction()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var param = Expression.Property(Expression.Parameter(typeof(TestClass), "t"), nameof(TestClass.Tags));
        var method = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 1)
            .MakeGenericMethod(typeof(string));
        var expr = Expression.Call(method, param);
        processor.Process(expr as MethodCallExpression);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void ProcessEnumerableContains_ShouldAddWhereAction()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);
        var tags = new[] { "a", "b" };
        var param = Expression.Constant(tags);
        var value = Expression.Property(Expression.Parameter(typeof(TestClass), "t"), nameof(TestClass.Name));
        var method = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));
        var expr = Expression.Call(method, param, value);
        processor.Process(expr as MethodCallExpression);
        context.Received().AddParameter(nameof(TestClass.Name), tags);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
    }
}
