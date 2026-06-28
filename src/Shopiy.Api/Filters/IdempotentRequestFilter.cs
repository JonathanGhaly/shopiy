using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StackExchange.Redis;
using System.Text.Json;

namespace Shopiy.Api.Filters;

public sealed class IdempotentRequestFilter : IAsyncActionFilter
{
    private const string IdempotencyHeaderName = "X-Idempotency-Key";
    private readonly IDatabase _redisDb;
    private readonly ILogger<IdempotentRequestFilter> _logger;

    public IdempotentRequestFilter(IConnectionMultiplexer redis, ILogger<IdempotentRequestFilter> logger)
    {
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // 1. Read header X-Idempotency-Key
        if (!httpContext.Request.Headers.TryGetValue(IdempotencyHeaderName, out var headerValues) || 
            string.IsNullOrWhiteSpace(headerValues.ToString()))
        {
            _logger.LogWarning("Idempotency check failed: Missing header '{HeaderName}'", IdempotencyHeaderName);
            context.Result = CreateBadRequestResult($"Missing required header '{IdempotencyHeaderName}'.");
            return;
        }

        var key = headerValues.ToString();

        // 2. Validate it is a valid UUID/GUID
        if (!Guid.TryParse(key, out var parsedGuid))
        {
            _logger.LogWarning("Idempotency check failed: Invalid UUID format in header '{HeaderName}' = '{Value}'", IdempotencyHeaderName, key);
            context.Result = CreateBadRequestResult($"Header '{IdempotencyHeaderName}' must be a valid UUID.");
            return;
        }

        // Standardized cache key pattern
        var redisKey = $"idempotency:{parsedGuid:D}";

        // Check if key exists
        var cachedValue = await _redisDb.StringGetAsync(redisKey);

        if (cachedValue.HasValue)
        {
            var cachedRecord = JsonSerializer.Deserialize<IdempotentRecord>(cachedValue.ToString());
            if (cachedRecord != null)
            {
                if (cachedRecord.State == "processing")
                {
                    _logger.LogWarning("Duplicate submission detected. Request is already processing for key: {IdempotencyKey}", parsedGuid);
                    context.Result = CreateConflictResult("Duplicate submission detected.");
                    return;
                }
                
                if (cachedRecord.State == "completed")
                {
                    _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", parsedGuid);
                    context.Result = new ContentResult
                    {
                        StatusCode = cachedRecord.StatusCode ?? 200,
                        Content = cachedRecord.ResponseBodyJson,
                        ContentType = "application/json"
                    };
                    return;
                }
            }
        }

        // Atomically set key as "processing" with a 5-minute TTL
        var initialRecord = new IdempotentRecord { State = "processing" };
        var initialJson = JsonSerializer.Serialize(initialRecord);
        var isSet = await _redisDb.StringSetAsync(
            redisKey, 
            initialJson, 
            expiry: TimeSpan.FromMinutes(5), 
            when: When.NotExists);

        if (!isSet)
        {
            _logger.LogWarning("Duplicate submission detected (race condition). Key: {IdempotencyKey}", parsedGuid);
            context.Result = CreateConflictResult("Duplicate submission detected.");
            return;
        }

        ActionExecutedContext executedContext;
        try
        {
            executedContext = await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Downstream execution failed with exception. Evicting idempotency key: {IdempotencyKey}", parsedGuid);
            await _redisDb.KeyDeleteAsync(redisKey);
            throw;
        }

        // Check if downstream execution failed with exception
        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
        {
            _logger.LogWarning(executedContext.Exception, "Downstream execution failed with unhandled exception. Evicting idempotency key: {IdempotencyKey}", parsedGuid);
            await _redisDb.KeyDeleteAsync(redisKey);
            return;
        }

        // Check response status code if it's an error result (>= 400)
        var statusCode = GetStatusCode(executedContext.Result);
        if (statusCode >= 400)
        {
            _logger.LogWarning("Downstream execution returned error status {StatusCode}. Evicting idempotency key: {IdempotencyKey}", statusCode, parsedGuid);
            await _redisDb.KeyDeleteAsync(redisKey);
            return;
        }

        // Downstream execution was successful -> Cache the response
        var successRecord = new IdempotentRecord
        {
            State = "completed",
            StatusCode = statusCode,
            ResponseBodyJson = GetResponseBodyJson(executedContext.Result)
        };
        var successJson = JsonSerializer.Serialize(successRecord);

        // Update the key in Redis (maintaining a 5-minute window for duplicate requests)
        await _redisDb.StringSetAsync(redisKey, successJson, expiry: TimeSpan.FromMinutes(5));
    }

    private static int GetStatusCode(IActionResult? result)
    {
        if (result is null) return 200;
        return result switch
        {
            ObjectResult obj => obj.StatusCode ?? 200,
            StatusCodeResult sc => sc.StatusCode,
            ContentResult cr => cr.StatusCode ?? 200,
            _ => 200
        };
    }

    private static string? GetResponseBodyJson(IActionResult? result)
    {
        if (result is null) return null;
        return result switch
        {
            ObjectResult obj => JsonSerializer.Serialize(obj.Value),
            ContentResult cr => cr.Content,
            _ => null
        };
    }

    private static IActionResult CreateBadRequestResult(string message)
    {
        return new BadRequestObjectResult(new
        {
            error = new
            {
                code = "BAD_REQUEST",
                message,
                details = new { }
            }
        });
    }

    private static IActionResult CreateConflictResult(string message)
    {
        return new ConflictObjectResult(new
        {
            error = new
            {
                code = "CONFLICT",
                message,
                details = new { }
            }
        });
    }
}

public sealed class IdempotentRecord
{
    public string State { get; set; } = "processing";
    public int? StatusCode { get; set; }
    public string? ResponseBodyJson { get; set; }
}
