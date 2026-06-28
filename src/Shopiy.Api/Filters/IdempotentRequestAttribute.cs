using Microsoft.AspNetCore.Mvc;

namespace Shopiy.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class IdempotentRequestAttribute : TypeFilterAttribute
{
    public IdempotentRequestAttribute() : base(typeof(IdempotentRequestFilter))
    {
    }
}
