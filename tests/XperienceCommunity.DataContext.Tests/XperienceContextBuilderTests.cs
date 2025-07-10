using CMS.ContentEngine;
using CMS.Websites;
using Microsoft.Extensions.DependencyInjection;
using XperienceCommunity.DataContext;
using XperienceCommunity.DataContext.Configurations;
using XperienceCommunity.DataContext.Interfaces;
using Xunit;

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
    [Fact]
    public void AddContentItemProcessor_ShouldRegisterProcessor()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var builder = new XperienceContextBuilder(services);

        builder.AddContentItemProcessor<FakeContent, FakeContentProcessor>();

        var provider = services.BuildServiceProvider();
        var processor = provider.GetService<IContentItemProcessor<FakeContent>>();
        Assert.NotNull(processor);
        Assert.IsType<FakeContentProcessor>(processor);
    }

    [Fact]
    public void AddPageContentProcessor_ShouldRegisterProcessor()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var builder = new XperienceContextBuilder(services);

        builder.AddPageContentProcessor<FakePage, FakePageProcessor>();

        var provider = services.BuildServiceProvider();
        var processor = provider.GetService<IPageContentProcessor<FakePage>>();
        Assert.NotNull(processor);
        Assert.IsType<FakePageProcessor>(processor);
    }

    [Fact]
    public void SetCacheTimeout_ShouldSetConfigAndRegisterSingleton()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var builder = new XperienceContextBuilder(services);

        builder.SetCacheTimeout(42);

        var provider = services.BuildServiceProvider();
        var config = provider.GetService<XperienceDataContextConfig>();
        Assert.NotNull(config);
        Assert.Equal(42, config.CacheTimeOut);
    }

    // Fake types for testing
    private class FakeContent : IContentItemFieldsSource
    {
        public ContentItemFields SystemFields => throw new NotImplementedException();
    }
    private class FakeContentProcessor : IContentItemProcessor<FakeContent>
    {
        public int Order => throw new NotImplementedException();

        public Task ProcessAsync(FakeContent content, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
    private class FakePage : IWebPageFieldsSource
    {
        public WebPageFields SystemFields => throw new NotImplementedException();
    }
    private class FakePageProcessor : IPageContentProcessor<FakePage>
    {
        public int Order => throw new NotImplementedException();

        public Task ProcessAsync(FakePage content, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

}
