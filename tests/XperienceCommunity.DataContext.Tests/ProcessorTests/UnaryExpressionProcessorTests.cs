using NSubstitute;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class UnaryExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();

        var processor = new Processors.UnaryExpressionProcessor(context);

        Assert.NotNull(processor);
    }
}
