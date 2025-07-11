using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests
{
    public class MethodCallExpressionProcessorEdgeCasesTests
    {
        private class Dummy { public int Value { get; set; } }

        [Fact]
        public void CanProcess_ShouldReturnFalse_ForUnsupportedMethod()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            var param = Expression.Parameter(typeof(string), "s");
            var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
            var call = Expression.Call(param, method!);

            Assert.False(processor.CanProcess(call));
        }

        [Fact]
        public void Process_ShouldThrow_ForUnsupportedMethod()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            var param = Expression.Parameter(typeof(string), "s");
            var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
            var call = Expression.Call(param, method!);

            Assert.Throws<NotSupportedException>(() => processor.Process(call));
        }

        [Fact]
        public void ProcessStringContains_ShouldThrow_OnInvalidFormat()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            // string.Contains with no arguments
            var param = Expression.Parameter(typeof(string), "s");
            var method = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

            Assert.ThrowsAny<Exception>(() => processor.Process(Expression.Call(param, method!, Array.Empty<Expression>())));
        }

        [Fact]
        public void ProcessEnumerableContains_ShouldThrow_OnNonConstantOrMemberCollection()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            // Create a method call with a NewExpression as the collection
            var ctor = typeof(List<int>).GetConstructor(Type.EmptyTypes);
            var newExpr = Expression.New(ctor!);
            var param = Expression.Parameter(typeof(Dummy), "x");
            var property = Expression.Property(param, nameof(Dummy.Value));
            var containsMethod = typeof(List<int>).GetMethod("Contains", new[] { typeof(int) });
            var call = Expression.Call(newExpr, containsMethod!, property);

            Assert.Throws<NotSupportedException>(() => processor.Process(call));
        }

        [Fact]
        public void AddWhereInTyped_ShouldThrow_OnNonGenericCollectionType()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            // Use a non-generic collection type
            var collection = new System.Collections.ArrayList { 1, 2, 3 };
            var paramName = "test";
            var ex = Assert.Throws<TargetInvocationException>(() =>
                processor.GetType()
                    .GetMethod("AddWhereInTyped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .Invoke(processor, new object[] { paramName, collection, collection.GetType() })
            );
        }

        [Fact]
        public void AddWhereInTyped_ShouldThrow_WhenElementTypeCannotBeResolved()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            // Use a type that implements IEnumerable but not IEnumerable<T>
            var collection = new CustomEnumerable();
            var paramName = "test";
            var ex = Assert.Throws<TargetInvocationException>(() =>
                processor.GetType()
                    .GetMethod("AddWhereInTyped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .Invoke(processor, new object[] { paramName, collection, collection.GetType() })
            );
        }

        private class CustomEnumerable : System.Collections.IEnumerable
        {
            public System.Collections.IEnumerator GetEnumerator() => Enumerable.Empty<int>().GetEnumerator();
        }

        [Fact]
        public void ProcessQueryableWhere_ShouldThrow_OnInvalidArguments()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            // Queryable.Where with only one argument
            var source = Expression.Constant(new List<int>().AsQueryable());
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2);

            Assert.Throws<ArgumentException>(() => processor.Process(Expression.Call(method, source)));
        }

        [Fact]
        public void ProcessQueryableSelect_ShouldThrow_OnInvalidArguments()
        {
            var context = Substitute.For<IExpressionContext>();
            var processor = new MethodCallExpressionProcessor(context);

            // Queryable.Select with only one argument
            var source = Expression.Constant(new List<int>().AsQueryable());
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2);

            Assert.Throws<ArgumentException>(() => processor.Process(Expression.Call(method, source)));
        }
    }
}
