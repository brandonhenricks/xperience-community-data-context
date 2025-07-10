using NSubstitute;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class EqualityExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();

        var processor = new Processors.EqualityExpressionProcessor(context);

        Assert.NotNull(processor);
    }
}
