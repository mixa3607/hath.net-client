namespace ArkProjects.Hath.WebService.Options;

/// <summary>
/// Web security options
/// </summary>
public class WebSecurityOptions
{
    /// <summary>
    /// Enable forwarded headers like X-Forwarded-For
    /// </summary>
    public bool EnableForwardedHeaders { get; set; }

    /// <summary>
    /// Enable rate limiter
    /// </summary>
    public bool EnableRateLimiter { get; set; }

    /// <summary>
    /// Enable request logging
    /// </summary>
    public bool EnableRequestLogging { get; set; }
}