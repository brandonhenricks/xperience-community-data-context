using NSubstitute;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class LogicalExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.LogicalExpressionProcessor(context, true);
        Assert.NotNull(processor);
    }
}
