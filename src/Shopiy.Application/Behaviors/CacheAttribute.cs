using System;

namespace Shopiy.Application.Behaviors
{
    /// <summary>
    /// Apply to MediatR requests to enable caching of the response.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CacheAttribute : Attribute
    {
        /// <summary>
        /// Time-to-live for the cached entry.
        /// </summary>
        public TimeSpan AbsoluteExpirationRelativeToNow { get; }

        public CacheAttribute(int seconds = 60)
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds);
        }
    }
}
