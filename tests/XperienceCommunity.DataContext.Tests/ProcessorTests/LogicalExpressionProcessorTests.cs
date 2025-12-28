using System.Linq.Expressions;
using CMS.ContentEngine;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class LogicalExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new LogicalExpressionProcessor(context, true);
        Assert.NotNull(processor);
    }

    [Fact]
    public void Constructor_WithVisitFunction_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        Expression visitFunction(Expression expr) => expr;
        var processor = new LogicalExpressionProcessor(context, true, visitFunction);
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
        var processor = new LogicalExpressionProcessor(context, isAnd);

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
        var processor = new LogicalExpressionProcessor(context, true);

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
        var processor = new LogicalExpressionProcessor(context, isAnd);

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
        var processor = new LogicalExpressionProcessor(context, true);

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
        var processor = new LogicalExpressionProcessor(context, false);

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
        var processor = new LogicalExpressionProcessor(context, true);

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
        var processor = new LogicalExpressionProcessor(context, true);

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

        var processor = new LogicalExpressionProcessor(context, true, visitFunction);

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
        var processor = new LogicalExpressionProcessor(context, true, visit);

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
        var processor = new LogicalExpressionProcessor(context, true); // AND processor

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
        var processor = new LogicalExpressionProcessor(context, true);

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
        var processor = new LogicalExpressionProcessor(context, false);

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
        var processor = new LogicalExpressionProcessor(context, true);

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
        var processor = new LogicalExpressionProcessor(context, true);

        var left = Expression.Constant(true); // Processable boolean constant
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive)); // Processable boolean property
        var binary = Expression.AndAlso(left, right);

        var result = processor.CanProcess(binary);

        Assert.True(result); // Should return true because at least one operand is processable
    }

    [Fact]
    public void Process_ShouldHandleNullValues_InComplexExpressions()
    {
        var context = Substitute.For<IExpressionContext>();
        // Provide a visit function to handle the Equal expression
        Expression visit(Expression expr) => expr;
        var processor = new LogicalExpressionProcessor(context, true, visit);

        var left = Expression.Equal(
            Expression.Property(Expression.Parameter(typeof(TestClass), "x"), nameof(TestClass.Name)),
            Expression.Constant(null, typeof(string))
        );
        var right = Expression.Property(Expression.Parameter(typeof(TestClass), "y"), nameof(TestClass.IsActive));
        var binary = Expression.AndAlso(left, right);

        processor.Process(binary);

        context.Received().PushLogicalGrouping("AND");
        context.Received().PopLogicalGrouping(); // Verify cleanup
    }

    [Fact]
    public void Process_ShouldHandleEmptyCollections_InContainsExpressions()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);

        // Test with empty List<int>
        var emptyList = new List<int>();
        var param = Expression.Parameter(typeof(TestClass), "x");
        var property = Expression.Property(param, nameof(TestClass.Age));
        var containsMethod = typeof(List<int>).GetMethod("Contains", new[] { typeof(int) });
        var call = Expression.Call(Expression.Constant(emptyList, typeof(List<int>)), containsMethod!, property);

        Assert.True(processor.CanProcess(call));
        processor.Process(call);

        // Should add a where action, but the action should represent an always-false condition
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());

        // Test with empty HashSet<string>
        var emptySet = new HashSet<string>();
        var stringProperty = Expression.Property(param, nameof(TestClass.Name));
        var containsStringMethod = typeof(HashSet<string>).GetMethod("Contains", new[] { typeof(string) });
        var callString = Expression.Call(Expression.Constant(emptySet, typeof(HashSet<string>)), containsStringMethod!, stringProperty);

        Assert.True(processor.CanProcess(callString));
        processor.Process(callString);

        context.Received(2).AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Theory]
    [InlineData(typeof(HashSet<int>))]
    [InlineData(typeof(Queue<int>))]
    [InlineData(typeof(Stack<int>))]
    public void MethodCallProcessor_ShouldSupportVariousCollectionTypes(Type collectionType)
    {
        // Arrange
        var context = Substitute.For<IExpressionContext>();
        var processor = new MethodCallExpressionProcessor(context);

        // Create a strongly-typed List<T> to use for .Contains()
        var elementType = collectionType.GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");
        var value = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;

        if (addMethod != null)
        {
            var paramValue = value;
            if (paramValue == null && elementType == typeof(string))
            {
                paramValue = "test";
            }
            addMethod.Invoke(list, new[] { paramValue });
        }

        // Build a .Contains() expression using the strongly-typed List<T>
        var param = Expression.Parameter(typeof(LogicalExpressionProcessorTests.TestClass), "x");
        var property = Expression.Property(param, nameof(LogicalExpressionProcessorTests.TestClass.Age));
        var containsMethod = listType.GetMethod("Contains");
        var call = Expression.Call(Expression.Constant(list), containsMethod!, property);

        // Act & Assert
        Assert.True(processor.CanProcess(call));
        processor.Process(call);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Age { get; set; }
    }
}
