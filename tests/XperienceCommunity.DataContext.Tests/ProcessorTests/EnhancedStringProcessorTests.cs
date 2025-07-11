using System.Linq.Expressions;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class EnhancedStringProcessorTests
{
    [Fact]
    public void CanProcess_ShouldReturnTrue_ForStringContains()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedStringProcessor(context);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
        var methodCall = Expression.Call(member, method!, Expression.Constant("test"));

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForStringStartsWith()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedStringProcessor(context);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) });
        var methodCall = Expression.Call(member, method!, Expression.Constant("test"));

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForStringEndsWith()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedStringProcessor(context);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) });
        var methodCall = Expression.Call(member, method!, Expression.Constant("string"));

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnTrue_ForStringIsNullOrEmpty()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedStringProcessor(context);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.IsNullOrEmpty));
        var methodCall = Expression.Call(method!, member);

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanProcess_ShouldReturnFalse_ForNonStringMethod()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedStringProcessor(context);
        
        var list = Expression.Constant(new List<int> { 1, 2, 3 });
        var method = typeof(List<int>).GetProperty(nameof(List<int>.Count))?.GetGetMethod();
        var methodCall = Expression.Call(list, method!);

        // Act
        var result = processor.CanProcess(methodCall);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Process_ShouldHandleStringContains()
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new EnhancedStringProcessor(context);
        
        var param = Expression.Parameter(typeof(TestClass), "x");
        var member = Expression.Property(param, nameof(TestClass.Name));
        var method = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
        var methodCall = Expression.Call(member, method!, Expression.Constant("test"));

        // Act
        processor.Process(methodCall);

        // Assert
        context.Received().AddParameter(Arg.Any<string>(), "test");
        context.Received().AddWhereAction(Arg.Any<Action<CMS.ContentEngine.WhereParameters>>());
    }

    private class TestClass
    {
        public string Name { get; set; } = "";
    }
}
