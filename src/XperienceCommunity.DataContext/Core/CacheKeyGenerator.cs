using System.Collections.Concurrent;
using System.Text;
using CMS.ContentEngine;

namespace XperienceCommunity.DataContext.Core;

/// <summary>
/// Provides utilities for generating consistent and efficient cache keys.
/// </summary>
internal static class CacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for content queries using HashCode.Combine for efficient hashing.
    /// </summary>
    /// <param name="contentType">The content type name.</param>
    /// <param name="identifier">The channel identifier (ID or name).</param>
    /// <param name="language">The language code.</param>
    /// <param name="queryBuilder">The query builder instance.</param>
    /// <param name="parameters">The query parameters.</param>
    /// <returns>A consistent cache key string.</returns>
    public static string GenerateCacheKey(
        string contentType,
        string identifier,
        string? language,
        ContentItemQueryBuilder queryBuilder,
        ConcurrentDictionary<string, object?> parameters)
    {
        // Build a single hash from all structural components to reduce collision risk
        var hashCode = new HashCode();

        // Core identity components
        hashCode.Add(contentType);
        hashCode.Add(identifier);
        hashCode.Add(language ?? string.Empty);

        // Use a structural representation of the query builder if available
        hashCode.Add(queryBuilder.ToString() ?? string.Empty);

        // Fold parameters directly into the same hash, sorted for order independence
        if (parameters.Count > 0)
        {
            var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal);
            foreach (var param in sortedParams)
            {
                hashCode.Add(param.Key);
                hashCode.Add(param.Value);
            }
        }

        var combinedHash = hashCode.ToHashCode();
        // Return a structured cache key with the combined hash
        return $"data|{contentType}|{identifier}|{language ?? "default"}|{combinedHash}";
    }

    /// <summary>
    /// Computes a hash for query parameters using HashCode.Combine.
    /// </summary>
    /// <param name="parameters">The parameters dictionary.</param>
    /// <returns>A hash code representing the parameters.</returns>
    private static int ComputeParametersHash(ConcurrentDictionary<string, object?> parameters)
    {
        if (parameters.Count == 0)
        {
            return 0;
        }

        // Sort parameters by key for consistent hashing regardless of insertion order
        var sortedParams = parameters.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
        
        // Build hash using HashCode for efficiency
        var hashCode = new HashCode();
        foreach (var param in sortedParams)
        {
            hashCode.Add(param.Key);
            hashCode.Add(param.Value);
        }
        
        return hashCode.ToHashCode();
    }
}
