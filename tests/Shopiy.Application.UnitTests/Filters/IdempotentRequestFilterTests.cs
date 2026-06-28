using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Shopiy.Api.Filters;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace Shopiy.Application.UnitTests.Filters;

public class IdempotentRequestFilterTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<ILogger<IdempotentRequestFilter>> _loggerMock;
    private readonly IdempotentRequestFilter _filter;

    public IdempotentRequestFilterTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _dbMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<IdempotentRequestFilter>>();

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _filter = new IdempotentRequestFilter(_redisMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Filter_Should_ReturnBadRequest_When_HeaderIsMissing()
    {
        var context = CreateActionExecutingContext(new HeaderDictionary());
        await _filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));
        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    [Fact]
    public async Task Filter_Should_ReturnConflict_When_RedisKeyIsProcessing()
    {
        var validGuid = Guid.NewGuid().ToString();
        var headers = new HeaderDictionary { { "X-Idempotency-Key", validGuid } };
        var context = CreateActionExecutingContext(headers);

        var processingRecordJson = JsonSerializer.Serialize(new IdempotentRecord { State = "processing" });
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(processingRecordJson);

        await _filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));
        Assert.IsType<ConflictObjectResult>(context.Result);
    }

    [Fact]
    public async Task Filter_Should_ReturnCachedResponse_When_RedisKeyIsCompleted()
    {
        var validGuid = Guid.NewGuid().ToString();
        var headers = new HeaderDictionary { { "X-Idempotency-Key", validGuid } };
        var context = CreateActionExecutingContext(headers);

        var completedRecordJson = JsonSerializer.Serialize(new IdempotentRecord 
        { 
            State = "completed", 
            StatusCode = 201, 
            ResponseBodyJson = "{\"orderId\":\"123\"}" 
        });
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(completedRecordJson);

        await _filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));
        
        var result = Assert.IsType<ContentResult>(context.Result);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("{\"orderId\":\"123\"}", result.Content);
    }

    [Fact]
    public async Task Filter_Should_EvictKey_When_DownstreamThrowsException()
    {
        var validGuid = Guid.NewGuid().ToString();
        var headers = new HeaderDictionary { { "X-Idempotency-Key", validGuid } };
        var context = CreateActionExecutingContext(headers);

        _dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _filter.OnActionExecutionAsync(context, () => throw new InvalidOperationException("DB Timeout"));
        });

        _dbMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString().Contains(validGuid)), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task Filter_Should_EvictKey_When_DownstreamReturnsErrorResult()
    {
        var validGuid = Guid.NewGuid().ToString();
        var headers = new HeaderDictionary { { "X-Idempotency-Key", validGuid } };
        var context = CreateActionExecutingContext(headers);

        _dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>())).ReturnsAsync(true);

        var executedContext = new ActionExecutedContext(context, new List<IFilterMetadata>(), new object())
        {
            Result = new BadRequestObjectResult("Validation failed")
        };

        await _filter.OnActionExecutionAsync(context, () => Task.FromResult(executedContext));

        _dbMock.Verify(d => d.KeyDeleteAsync(It.Is<RedisKey>(k => k.ToString().Contains(validGuid)), It.IsAny<CommandFlags>()), Times.Once);
    }

    private static ActionExecutingContext CreateActionExecutingContext(HeaderDictionary headers)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var header in headers)
        {
            httpContext.Request.Headers[header.Key] = header.Value;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    }
}
