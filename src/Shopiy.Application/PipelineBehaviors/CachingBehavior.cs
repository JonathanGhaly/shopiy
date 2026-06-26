using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Shopiy.Application.Behaviors;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.PipelineBehaviors
{
    /// <summary>
    /// Caches the response of a MediatR request when the request type is annotated with <see cref="CacheAttribute"/>.
    /// The cache key is generated from the request type name and a hash of its public property values.
    /// </summary>
    public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : class
    {
        private readonly ICacheService _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Look for CacheAttribute on the request type
            var cacheAttr = Attribute.GetCustomAttribute(typeof(TRequest), typeof(CacheAttribute)) as CacheAttribute;
            if (cacheAttr == null)
            {
                // No caching requested – just continue
                return await next();
            }

            var cacheKey = GenerateCacheKey(request);
            try
            {
                var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit for {Key}", cacheKey);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for {Key}", cacheKey);
            }

            // Cache miss – execute handler
            var response = await next();
            try
            {
                await _cache.SetAsync(cacheKey, response, cacheAttr.AbsoluteExpirationRelativeToNow, cancellationToken);
                _logger.LogInformation("Cache set for {Key}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache set failed for {Key}", cacheKey);
            }

            return response;
        }

        private static string GenerateCacheKey(TRequest request)
        {
            // Simple deterministic key: type name + JSON of the request (excluding nulls)
            var json = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            return $"{typeof(TRequest).FullName}:{json}";
        }
    }
}
