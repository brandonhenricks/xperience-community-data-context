using System.Linq.Expressions;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class EnhancedCollectionProcessorTests
{
    [Fact]
    public void CanProcess_ShouldReturnTrue_ForEnumerableContains()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedCollectionProcessor(context);
        
        var collection = Expression.Constant(new[] { 1, 2, 3 });
        var value = Expression.Constant(2);
        var method = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(int));
        var methodCall = Expression.Call(method, collection, value);

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForInstanceContains()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedCollectionProcessor(context);
        
        var collection = Expression.Constant(new List<int> { 1, 2, 3 });
        var value = Expression.Constant(2);
        var method = typeof(List<int>).GetMethod(nameof(List<int>.Contains), new[] { typeof(int) });
        var methodCall = Expression.Call(collection, method!, value);

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonCollectionMethod()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedCollectionProcessor(context);
        
        var str = Expression.Constant("test");
        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
        var methodCall = Expression.Call(str, method!);

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Process_ShouldHandleStaticContains()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedCollectionProcessor(context);
        
        var collection = Expression.Constant(new[] { 1, 2, 3 });
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Value));
        var method = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(int));
        var methodCall = Expression.Call(method, collection, member);

        // Act
        processor.Process(methodCall);

        // Assert
        context.Received().AddParameter(Arg.Any<string>(), Arg.Any<object>());
        context.Received().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    [Fact]
    public void Process_ShouldHandleInstanceContains()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedCollectionProcessor(context);
        
        var collection = Expression.Constant(new List<int> { 1, 2, 3 });
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Value));
        var method = typeof(List<int>).GetMethod(nameof(List<int>.Contains), new[] { typeof(int) });
        var methodCall = Expression.Call(collection, method!, member);

        // Act
        processor.Process(methodCall);

        // Assert
        context.Received().AddParameter(Arg.Any<string>(), Arg.Any<object>());
        context.Received().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    private class TestClass
    {
        public int Value { get; set; }
    }
}
