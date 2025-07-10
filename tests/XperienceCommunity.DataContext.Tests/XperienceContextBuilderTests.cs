using Xunit;
using XperienceCommunity.DataContext;

namespace XperienceCommunity.DataContext.Tests;

public class XperienceContextBuilderTests
{
    [Fact]
    public void Constructor_ShouldInstantiate()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var builder = new XperienceContextBuilder(services);
        Assert.NotNull(builder);
    }
}
