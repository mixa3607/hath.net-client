using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ArkProjects.Hath.WebService.Misc;

internal sealed class IpRateLimiterPolicy : IRateLimiterPolicy<IPAddress>
{
    private readonly Func<HttpContext, RateLimitPartition<IPAddress>> _partitioner;

    public IpRateLimiterPolicy(Func<HttpContext, RateLimitPartition<IPAddress>> partitioner,
        Func<OnRejectedContext, CancellationToken, ValueTask>? onRejected)
    {
        _partitioner = partitioner;
        OnRejected = onRejected;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

    public RateLimitPartition<IPAddress> GetPartition(HttpContext httpContext)
    {
        return _partitioner(httpContext);
    }
}