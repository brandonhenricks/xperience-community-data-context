using System.Linq.Expressions;
using CMS.ContentEngine;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Exceptions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class EqualityExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();

        var processor = new EqualityExpressionProcessor(context);

        Assert.NotNull(processor);
    }

    [Fact]
    public void Process_MemberEqualsConstant_AddsParameterAndWhereEquals()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var member = Expression.Property(Expression.Parameter(typeof(string), "x"), "Length");
        var constant = Expression.Constant(5);
        var binary = Expression.Equal(member, constant);

        processor.Process((BinaryExpression)binary);

        context.Received().AddParameter("Length", 5);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ConstantEqualsMember_AddsParameterAndWhereEquals()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var member = Expression.Property(Expression.Parameter(typeof(string), "x"), "Length");
        var constant = Expression.Constant(5);
        var binary = Expression.Equal(constant, member);

        processor.Process((BinaryExpression)binary);

        context.Received().AddParameter("Length", 5);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_MemberEqualsUnaryConstant_AddsParameterAndWhereEquals()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var member = Expression.Property(Expression.Parameter(typeof(string), "x"), "Length");
        var constant = Expression.Constant(5);
        var unary = Expression.Convert(constant, typeof(int));
        var binary = Expression.Equal(member, unary);

        processor.Process((BinaryExpression)binary);

        context.Received().AddParameter("Length", 5);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_UnaryConstantEqualsMember_AddsParameterAndWhereEquals()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var member = Expression.Property(Expression.Parameter(typeof(string), "x"), "Length");
        var constant = Expression.Constant(5);
        var unary = Expression.Convert(constant, typeof(int));
        var binary = Expression.Equal(unary, member);

        processor.Process((BinaryExpression)binary);

        context.Received().AddParameter("Length", 5);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_MemberEqualsMember_AddsParameterAndWhereEquals()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var param = Expression.Constant("hello");
        var leftMember = Expression.Property(param, "Length");
        var rightMember = Expression.Property(Expression.Constant("world!"), "Length");
        var binary = Expression.Equal(leftMember, rightMember);

        processor.Process((BinaryExpression)binary);

        context.Received().AddParameter("Length", 6);
        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_ConstantEqualsConstant_AddsWhereEqualsWithResult()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var left = Expression.Constant(5);
        var right = Expression.Constant(5);
        var binary = Expression.Equal(left, right);

        processor.Process((BinaryExpression)binary);

        context.Received().AddWhereAction(Arg.Any<Action<WhereParameters>>());
    }

    [Fact]
    public void Process_InvalidExpression_ThrowsInvalidExpressionFormatException()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        var left = Expression.Parameter(typeof(int), "x");
        var right = Expression.Parameter(typeof(int), "y");
        var binary = Expression.Add(left, right);

        Assert.Throws<InvalidExpressionFormatException>(() =>
            processor.Process((BinaryExpression)binary));
    }

    [Theory]
    [InlineData(typeof(MemberExpression), typeof(ConstantExpression), true)]
    [InlineData(typeof(ConstantExpression), typeof(MemberExpression), true)]
    [InlineData(typeof(MemberExpression), typeof(UnaryExpression), true)]
    [InlineData(typeof(UnaryExpression), typeof(MemberExpression), true)]
    [InlineData(typeof(MemberExpression), typeof(MemberExpression), true)]
    [InlineData(typeof(ConstantExpression), typeof(ConstantExpression), false)]
    [InlineData(typeof(ConstantExpression), typeof(ParameterExpression), false)]
    public void CanProcess_ReturnsExpectedResult(Type leftType, Type rightType, bool expected)
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new EqualityExpressionProcessor(context);

        Expression left = leftType switch
        {
            Type t when t == typeof(ConstantExpression) => Expression.Constant(1),
            Type t when t == typeof(MemberExpression) => Expression.Property(Expression.Parameter(typeof(string), "x"), "Length"),
            Type t when t == typeof(UnaryExpression) => Expression.Convert(Expression.Constant(1), typeof(int)),
            Type t when t == typeof(ParameterExpression) => Expression.Parameter(typeof(int), "x"),
            _ => throw new NotSupportedException($"Type {leftType} not supported")
        };

        Expression right = rightType switch
        {
            Type t when t == typeof(ConstantExpression) => Expression.Constant(2),
            Type t when t == typeof(MemberExpression) => Expression.Property(Expression.Parameter(typeof(string), "y"), "Length"),
            Type t when t == typeof(UnaryExpression) => Expression.Convert(Expression.Constant(2), typeof(int)),
            Type t when t == typeof(ParameterExpression) => Expression.Parameter(typeof(int), "y"),
            _ => throw new NotSupportedException($"Type {rightType} not supported")
        };

        var binary = Expression.MakeBinary(ExpressionType.Equal, left, right);

        var result = processor.CanProcess(binary);

        Assert.Equal(expected, result);
    }
}
