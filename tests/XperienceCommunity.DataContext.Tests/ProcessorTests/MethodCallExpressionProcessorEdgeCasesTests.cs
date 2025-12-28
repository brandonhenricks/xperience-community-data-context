using System.Linq.Expressions;
using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests
{
    public class MethodCallExpressionProcessorEdgeCasesTests
    {
        private class Dummy
        { public int Value { get; set; } }

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
