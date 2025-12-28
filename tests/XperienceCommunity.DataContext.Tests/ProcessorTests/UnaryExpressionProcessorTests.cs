using NSubstitute;
using XperienceCommunity.DataContext.Abstractions;
using XperienceCommunity.DataContext.Expressions.Processors;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class UnaryExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();

        var processor = new UnaryExpressionProcessor(context);

        Assert.NotNull(processor);
    }
}
