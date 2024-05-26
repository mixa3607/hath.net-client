using System.Diagnostics;

namespace ArkProjects.Hath.WebService;

public static class Telemetry
{
    public const string ServiceName = "hath.NET";
    public static readonly ActivitySource ActivitySource = new ActivitySource(ServiceName);
}