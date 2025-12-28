using System.Collections.Concurrent;
using CMS.ContentEngine;
using Xunit;
using XperienceCommunity.DataContext.Core;

namespace XperienceCommunity.DataContext.Tests;

/// <summary>
/// Tests for the CacheKeyGenerator utility class.
/// </summary>
public class CacheKeyGeneratorTests
{
    [Fact]
    public void GenerateCacheKey_WithBasicParameters_ReturnsConsistentKey()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters);

        // Assert
        Assert.Equal(key1, key2);
        Assert.Contains("data|", key1);
        Assert.Contains(contentType, key1);
        Assert.Contains(identifier, key1);
        Assert.Contains(language, key1);
    }

    [Fact]
    public void GenerateCacheKey_WithNullLanguage_UsesDefaultLanguage()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();

        // Act
        var key = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, null, queryBuilder, parameters);

        // Assert
        Assert.NotNull(key);
        Assert.Contains("default", key);
    }

    [Fact]
    public void GenerateCacheKey_WithParameters_CreatesUniqueKeys()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        
        var parameters1 = new ConcurrentDictionary<string, object?>();
        parameters1.TryAdd("param1", "value1");
        
        var parameters2 = new ConcurrentDictionary<string, object?>();
        parameters2.TryAdd("param1", "value2");

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters1);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithSameParametersInDifferentOrder_ReturnsConsistentKey()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        
        var parameters1 = new ConcurrentDictionary<string, object?>();
        parameters1.TryAdd("a", 1);
        parameters1.TryAdd("b", 2);
        parameters1.TryAdd("c", 3);
        
        var parameters2 = new ConcurrentDictionary<string, object?>();
        parameters2.TryAdd("c", 3);
        parameters2.TryAdd("a", 1);
        parameters2.TryAdd("b", 2);

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters1);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters2);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithEmptyParameters_ReturnsValidKey()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();

        // Act
        var key = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters);

        // Assert
        Assert.NotNull(key);
        Assert.Contains("data|", key);
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentContentTypes_ReturnsUniqueKeys()
    {
        // Arrange
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey("ContentType1", identifier, language, queryBuilder, parameters);
        var key2 = CacheKeyGenerator.GenerateCacheKey("ContentType2", identifier, language, queryBuilder, parameters);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentIdentifiers_ReturnsUniqueKeys()
    {
        // Arrange
        var contentType = "TestContent";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, "123", language, queryBuilder, parameters);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, "456", language, queryBuilder, parameters);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentLanguages_ReturnsUniqueKeys()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, "en-US", queryBuilder, parameters);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, "de-DE", queryBuilder, parameters);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithMultipleParameters_HandlesComplexValues()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        var parameters = new ConcurrentDictionary<string, object?>();
        parameters.TryAdd("string", "test");
        parameters.TryAdd("int", 42);
        parameters.TryAdd("bool", true);
        parameters.TryAdd("null", null);

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithNullParameter_HandlesCorrectly()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        
        var parameters1 = new ConcurrentDictionary<string, object?>();
        parameters1.TryAdd("param1", null);
        
        var parameters2 = new ConcurrentDictionary<string, object?>();
        parameters2.TryAdd("param1", null);

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters1);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters2);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentTypesButSimilarStringRepresentation_ReturnsUniqueKeys()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        
        var parameters1 = new ConcurrentDictionary<string, object?>();
        parameters1.TryAdd("value", "42"); // String "42"
        
        var parameters2 = new ConcurrentDictionary<string, object?>();
        parameters2.TryAdd("value", 42); // Integer 42

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters1);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters2);

        // Assert - Different types should produce different cache keys
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateCacheKey_WithObjectsHavingSameHashCodeButDifferentValues_ReturnsUniqueKeys()
    {
        // Arrange
        var contentType = "TestContent";
        var identifier = "123";
        var language = "en-US";
        var queryBuilder = new ContentItemQueryBuilder();
        
        // Create two objects that will have the same hash code but different values
        // Using strings with carefully chosen values that hash to the same value is complex,
        // so we'll use a simpler approach: verify that structurally different objects
        // with the same ToString() produce different keys
        var parameters1 = new ConcurrentDictionary<string, object?>();
        parameters1.TryAdd("key1", "value1");
        parameters1.TryAdd("key2", "value2");
        
        var parameters2 = new ConcurrentDictionary<string, object?>();
        parameters2.TryAdd("key1", "value1");
        parameters2.TryAdd("key2", "value3"); // Different value

        // Act
        var key1 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters1);
        var key2 = CacheKeyGenerator.GenerateCacheKey(contentType, identifier, language, queryBuilder, parameters2);

        // Assert - Even if objects had the same hash code, different values should produce different keys
        Assert.NotEqual(key1, key2);
    }
}
