using CMS.ContentEngine;
using Microsoft.Extensions.Logging;
using NSubstitute;
using XperienceCommunity.DataContext.Executors;

namespace XperienceCommunity.DataContext.Tests.Executors
{
    public class ReusableSchemaExecutorTests
    {
        [Fact]
        public void Constructor_ShouldThrow_IfLoggerIsNull()
        {
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            Assert.Throws<ArgumentNullException>(() =>
                new ReusableSchemaExecutor<object>(null!, queryExecutor));
        }

        [Fact]
        public void Constructor_ShouldInstantiate()
        {
            var logger = Substitute.For<ILogger<ReusableSchemaExecutor<object>>>();
            var queryExecutor = Substitute.For<IContentQueryExecutor>();
            var executor = new ReusableSchemaExecutor<object>(logger, queryExecutor);
            Assert.NotNull(executor);
        }
    }
}
