using NSubstitute;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

namespace XperienceCommunity.DataContext.Tests.ProcessorTests;

public class MethodCallExpressionProcessorTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var context = Substitute.For<IExpressionContext>();
        var processor = new Processors.MethodCallExpressionProcessor(context);
        Assert.NotNull(processor);
    }
}
